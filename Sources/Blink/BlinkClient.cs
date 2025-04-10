using System;
using System.Linq;
using Blink.Models;
using System.Net.Http;
using Blink.Exceptions;
using System.Reflection;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Blink
{
    /// <summary>
    /// Blink client class implementing Blink API
    /// </summary>
    public class BlinkClient : IBlinkClient
    {
        /// <summary>
        /// Unique ID to avoid reauthorization by pin code
        /// </summary>
        public string UniqueId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// For some reason, their server returns empty response without this delay.
        /// You can control this delay by setting this property.
        /// Set to 0 to disable delay.
        /// </summary>
        public int GeneralSleepTime { get; set; } = 3500;

        private int? _clientId;
        private int? _accountId;
        private HttpClient? _http;


        /// <summary>
        /// Authorize with email and password provided in constructor.
        /// </summary>
        /// <returns><see cref="LoginResult"/> object with authorization data</returns>
        /// <exception cref="BlinkClientException">Thrown when email or password is not provided in constructor</exception>
        /// <exception cref="BlinkClientException">Thrown when authorization fails</exception>
        /// <exception cref="BlinkClientException">Thrown when no content is returned</exception>
        public async Task<LoginResult> AuthorizeAsync(string email, string password, bool reauth = true)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new BlinkClientException("Email is required to authorize");
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new BlinkClientException("Password is required to authorize");
            }

            string clientName = Assembly.GetEntryAssembly()!.GetName().Name + "_v" + Assembly.GetEntryAssembly()!.GetName().Version;
            var body = new
            {
                unique_id = UniqueId,
                email,
                password,
                client_name = clientName,
                reauth = reauth ? "true" : "false",

                //app_version = "",
                //client_type = "",
                //device_identifier = "Amazon ",
                //notification_key = "",
                //os_version = "",
            };
            const string _baseUrl = "https://rest-prod.immedia-semi.com";
            _http = CreateHttpClient(_baseUrl);
            var httpClient = GetHttpClient();
            var response = await httpClient.PostAsJsonAsync("/api/v5/account/login", body);
            if (!response.IsSuccessStatusCode)
            {
                // if has content, try to read error message
                var error = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrWhiteSpace(error))
                {
                    throw new BlinkClientException($"Failed to authorize - {response.StatusCode} ({response.ReasonPhrase}) - {error}");
                }
                throw new BlinkClientException($"Failed to authorize - {response.StatusCode} ({response.ReasonPhrase})");
            }
            var loginResult = await response.Content.ReadFromJsonAsync<LoginResult>()
                ?? throw new BlinkClientException("Failed to authorize - no content");
            if (!string.IsNullOrWhiteSpace(loginResult.Account.Tier) && !string.IsNullOrWhiteSpace(loginResult.Auth.Token))
            {
                string baseUrl = $"https://rest-{loginResult.Account.Tier}.immedia-semi.com";
                _http = CreateHttpClient(baseUrl, loginResult.Auth.Token);
            }
            else
            {
                throw new BlinkClientException("Failed to authorize - no token or tier in response");
            }
            _accountId = loginResult.Account.AccountId;
            _clientId = loginResult.Account.ClientId;
            if (_accountId == null || _clientId == null)
            {
                throw new BlinkClientException("Failed to authorize - no account ID or client ID in response");
            }
            return loginResult;
        }

        private HttpClient CreateHttpClient(string baseUrl, string? token = "")
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new BlinkClientException("Base URL is required to authorize");
            }
            _http = new HttpClient()
            {
                BaseAddress = new Uri(baseUrl)
            };
            string appBuild = "ANDROID_28746173";
            string userAgent = "35.1" + appBuild;
            _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);
            _http.DefaultRequestHeaders.TryAddWithoutValidation("APP-BUILD", appBuild);
            _http.DefaultRequestHeaders.TryAddWithoutValidation("LOCALE", "en_US");
            _http.DefaultRequestHeaders.TryAddWithoutValidation("X-Blink-Time-Zone", "UTC");
            if (!string.IsNullOrWhiteSpace(token))
            {
                _http.DefaultRequestHeaders.TryAddWithoutValidation("TOKEN-AUTH", token);
            }
            return _http;
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
                throw new BlinkClientException("Account ID is required to verify pin");
            }
            if (_clientId == null)
            {
                throw new BlinkClientException("Client ID is required to verify pin");
            }
            string url = $"/api/v4/account/{_accountId}/client/{_clientId}/pin/verify";
            var body = new
            {
                pin = code
            };
            var httpClient = GetHttpClient();
            var response = await httpClient.PostAsJsonAsync(url, body);
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
            var httpClient = GetHttpClient();
            var response = await httpClient.GetAsync(url);
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
            var httpClient = GetHttpClient();
            var result = await httpClient.PostAsync(url, null);
            if (!result.IsSuccessStatusCode)
            {
                throw new BlinkClientException("Failed to get videos - " + result.ReasonPhrase);
            }
            var manifestData = await result.Content.ReadFromJsonAsync<ManifestData>()
                ?? throw new BlinkClientException("Failed to get videos - no content");
            url += $"/{manifestData.Id}";
            await Task.Delay(GeneralSleepTime); // I don't know why, but their server returns empty response without this delay
            var response = await httpClient.GetAsync(url);
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
            var httpClient = GetHttpClient();
            while (count++ < tryCount)
            {
                await httpClient.PostAsync(url, null);
                await Task.Delay(GeneralSleepTime);

                response = await httpClient.GetAsync(url);
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
            var httpClient = GetHttpClient();
            await Task.Delay(GeneralSleepTime);
            var result = await httpClient.PostAsync(url, null);
            if (!result.IsSuccessStatusCode)
            {
                string error = await result.Content.ReadAsStringAsync();
                throw new BlinkClientException($"Failed to delete video {video.Id} - {result.ReasonPhrase} - {error}");
            }
            result.EnsureSuccessStatusCode();
        }

        private HttpClient GetHttpClient()
        {
            return _http ?? throw new BlinkClientException("Not authorized");
        }
    }
}
