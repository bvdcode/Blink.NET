﻿using System;
using System.Linq;
using Blink.Models;
using System.Net.Http;
using Blink.Exceptions;
using System.Reflection;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Mime;

namespace Blink
{
    /// <summary>
    /// Blink client class implementing Blink API
    /// </summary>
    public class BlinkClient
    {
        /// <summary>
        /// For some reason, their server returns empty response without this delay.
        /// You can control this delay by setting this property.
        /// Set to 0 to disable delay.
        /// </summary>
        public int GeneralSleepTime { get; set; } = 3500;

        private int? _clientId;
        private int? _accountId;
        private HttpClient _http;
        private readonly string _email;
        private readonly string _password;
        private readonly string _userAgent;
        private const string _baseUrl = "https://rest-prod.immedia-semi.com";

        /// <summary>
        /// Create Blink client with email and password.
        /// </summary>
        /// <param name="email">Email</param>
        /// <param name="password">Password</param>
        public BlinkClient(string email, string password)
        {
            _email = email;
            _password = password;
            _http = new HttpClient()
            {
                BaseAddress = new Uri(_baseUrl)
            };
            _userAgent = Assembly.GetEntryAssembly()!.GetName().Name + " v" + Assembly.GetEntryAssembly()!.GetName().Version;
            _http.DefaultRequestHeaders.UserAgent.ParseAdd(_userAgent);
        }

        /// <summary>
        /// Create Blink client with token, tier, client ID and account ID from 
        /// <see cref="BlinkAuthorizationData"/> object returned by <see cref="AuthorizeAsync"/> method.
        /// </summary>
        /// <param name="token">Authorization token</param>
        /// <param name="tier">Server API tier, ex. tier 'u018' will be 'https://rest-u018.immedia-semi.com'</param>
        /// <param name="clientId">Client ID</param>
        /// <param name="accountId">Account ID</param>
        public BlinkClient(string token, string tier, int clientId, int accountId)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new BlinkClientException("Token is required to authorize");
            }
            if (string.IsNullOrWhiteSpace(tier))
            {
                throw new BlinkClientException("Tier is required to authorize");
            }
            if (clientId == 0)
            {
                throw new BlinkClientException("Client ID is required to authorize");
            }
            if (accountId == 0)
            {
                throw new BlinkClientException("Account ID is required to authorize");
            }

            _email = string.Empty;
            _password = string.Empty;
            _clientId = clientId;
            _accountId = accountId;
            string baseUrl = $"https://rest-{tier}.immedia-semi.com";
            _http = new HttpClient()
            {
                BaseAddress = new Uri(baseUrl)
            };
            _http.DefaultRequestHeaders.Add("TOKEN-AUTH", token);
            _userAgent = "35.1ANDROID_28746173";
            _http.DefaultRequestHeaders.UserAgent.ParseAdd(_userAgent);
        }

        /// <summary>
        /// Authorize with email and password provided in constructor.
        /// </summary>
        /// <returns><see cref="BlinkAuthorizationData"/> object with authorization data</returns>
        /// <exception cref="BlinkClientException">Thrown when email or password is not provided in constructor</exception>
        /// <exception cref="BlinkClientException">Thrown when authorization fails</exception>
        /// <exception cref="BlinkClientException">Thrown when no content is returned</exception>
        public async Task<BlinkAuthorizationData> AuthorizeAsync()
        {
            if (string.IsNullOrWhiteSpace(_email) || string.IsNullOrWhiteSpace(_password))
            {
                throw new BlinkClientException("Email and password are required in constructor to authorize");
            }

            string appName = Assembly.GetEntryAssembly()!.GetName().Name + " v" + Assembly.GetEntryAssembly()!.GetName().Version;
            var body = new
            {
                unique_id = appName,
                email = _email,
                password = _password
            };
            var response = await _http.PostAsJsonAsync("/api/v5/account/login", body);
            if (!response.IsSuccessStatusCode)
            {
                throw new BlinkClientException("Failed to authorize - " + response.ReasonPhrase);
            }
            var loginResult = await response.Content.ReadFromJsonAsync<LoginResult>()
                ?? throw new BlinkClientException("Failed to authorize - no content");
            if (!string.IsNullOrWhiteSpace(loginResult.Account.Tier) && !string.IsNullOrWhiteSpace(loginResult.Auth.Token))
            {
                string baseUrl = $"https://rest-{loginResult.Account.Tier}.immedia-semi.com";
                _http = new HttpClient()
                {
                    BaseAddress = new Uri(baseUrl)
                };
                _http.DefaultRequestHeaders.Add("TOKEN-AUTH", loginResult.Auth.Token);
                _http.DefaultRequestHeaders.UserAgent.ParseAdd(_userAgent);
            }
            _accountId = loginResult.Account.AccountId;
            _clientId = loginResult.Account.ClientId;
            return new BlinkAuthorizationData
            {
                AccountId = loginResult.Account.AccountId,
                ClientId = loginResult.Account.ClientId,
                Tier = loginResult.Account.Tier,
                Token = loginResult.Auth.Token
            };
        }

        /// <summary>
        /// Verify pin code from text message received after authorization.
        /// </summary>
        /// <param name="code">Pin code from text message</param>
        /// <exception cref="BlinkClientException">Thrown when not authorized</exception>
        /// <exception cref="BlinkClientException">Thrown when pin verification fails</exception>
        public async Task VerifyPinAsync(string code)
        {
            if (_accountId == null)
            {
                throw new BlinkClientException("Not authorized");
            }
            string url = $"/api/v4/account/{_accountId}/client/{_clientId}/pin/verify";
            var body = new
            {
                pin = code
            };
            var response = await _http.PostAsJsonAsync(url, body);
            if (!response.IsSuccessStatusCode)
            {
                throw new BlinkClientException("Failed to verify pin - " + response.ReasonPhrase);
            }
        }

        /// <summary>
        /// Get dashboard data.
        /// </summary>
        /// <returns><see cref="Dashboard"/> object with dashboard data</returns>
        /// <exception cref="BlinkClientException">Thrown when not authorized</exception>
        /// <exception cref="BlinkClientException">Thrown when failed to get dashboard</exception>
        public async Task<Dashboard> GetDashboardAsync()
        {
            if (_accountId == null)
            {
                throw new BlinkClientException("Not authorized");
            }
            string url = $"/api/v3/accounts/{_accountId}/homescreen";
            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                throw new BlinkClientException("Failed to get dashboard - " + response.ReasonPhrase);
            }
            return await response.Content.ReadFromJsonAsync<Dashboard>()
                ?? throw new BlinkClientException("Failed to get dashboard - no content");
        }

        /// <summary>
        /// Get videos from Blink camera.
        /// </summary>
        /// <returns>Collection of <see cref="BlinkVideoInfo"/> objects with video data</returns>
        /// <exception cref="BlinkClientException">Thrown when not authorized</exception>
        /// <exception cref="BlinkClientException">Thrown when failed to get videos</exception>
        public async Task<IEnumerable<BlinkVideoInfo>> GetVideosAsync()
        {
            if (_accountId == null)
            {
                throw new BlinkClientException("Not authorized");
            }
            var dashboard = await GetDashboardAsync();
            var module = dashboard.SyncModules.FirstOrDefault()
                ?? throw new BlinkClientException("No sync modules found");
            string url = $"/api/v1/accounts/{_accountId}/networks/{module.NetworkId}/" +
                $"sync_modules/{module.Id}/local_storage/manifest/request";
            await Task.Delay(GeneralSleepTime); // I don't know why, but their server returns empty response without this delay
            var result = await _http.PostAsync(url, null);
            if (!result.IsSuccessStatusCode)
            {
                throw new BlinkClientException("Failed to get videos - " + result.ReasonPhrase);
            }
            var manifestData = await result.Content.ReadFromJsonAsync<ManifestData>()
                ?? throw new BlinkClientException("Failed to get videos - no content");
            url += $"/{manifestData.Id}";
            await Task.Delay(GeneralSleepTime); // I don't know why, but their server returns empty response without this delay
            var response = await _http.GetAsync(url);
            var videoResponse = await response.Content.ReadFromJsonAsync<VideoResponse>()
                ?? throw new BlinkClientException("Failed to get videos - no content");
            foreach (var video in videoResponse.Videos)
            {
                video.NetworkId = module.NetworkId;
                video.ModuleId = module.Id;
                video.ManifestId = videoResponse.ManifestId;
            }
            return videoResponse.Videos;
        }

        /// <summary>
        /// Get video from Blink camera.
        /// </summary>
        /// <param name="video"><see cref="BlinkVideoInfo"/> object with video data</param>
        /// <param name="tryCount">Number of tries to get video</param>
        /// <returns>Video as byte array</returns>
        /// <exception cref="BlinkClientException">Thrown when not authorized</exception>
        public async Task<byte[]> GetVideoAsync(BlinkVideoInfo video, int tryCount = 3)
        {
            if (_accountId == null)
            {
                throw new BlinkClientException("Not authorized");
            }
            if (video.NetworkId == 0 || video.ModuleId == 0 || string.IsNullOrWhiteSpace(video.ManifestId))
            {
                throw new BlinkClientException("Video data is not valid");
            }
            string url = $"/api/v1/accounts/{_accountId}/networks/{video.NetworkId}/" +
                $"sync_modules/{video.ModuleId}/local_storage/manifest/{video.ManifestId}/clip/request/{video.Id}";

            int count = 0;
            string contentType = string.Empty;
            HttpResponseMessage? response = null;
            while (count++ < tryCount)
            {
                await _http.PostAsync(url, null);
                await Task.Delay(GeneralSleepTime);

                response = await _http.GetAsync(url);
                contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
                if (contentType == "video/mp4")
                {
                    return await response.Content.ReadAsByteArrayAsync();
                }
            }
            throw new BlinkClientException($"Failed to get video {video.Id}, contentType {contentType} - {response?.ReasonPhrase ?? "Unknown Error"}");
        }

        /// <summary>
        /// Delete video from Blink camera.
        /// </summary>
        /// <param name="video"><see cref="BlinkVideoInfo"/> object with video data</param>
        /// <exception cref="BlinkClientException">Thrown when not authorized</exception>
        public async Task DeleteVideoAsync(BlinkVideoInfo video)
        {
            if (_accountId == null)
            {
                throw new BlinkClientException("Not authorized");
            }

            if (video.NetworkId == 0 || video.ModuleId == 0 || string.IsNullOrWhiteSpace(video.ManifestId))
            {
                throw new BlinkClientException("Video data is not valid");
            }
            string url = $"/api/v1/accounts/{_accountId}/networks/{video.NetworkId}/" +
                $"sync_modules/{video.ModuleId}/local_storage/manifest/{video.ManifestId}/clip/delete/{video.Id}";
            var result = await _http.PostAsync(url, null);
            result.EnsureSuccessStatusCode();
        }
    }
}
