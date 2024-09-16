using System;
using Blink.Models;
using System.Net.Http;
using Blink.Exceptions;
using System.Reflection;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Blink
{
    public class BlinkClient
    {
        private int? _clientId;
        private int? _accountId;
        private HttpClient _http;
        private readonly string _email;
        private readonly string _password;
        private readonly string _userAgent;
        private const string _baseUrl = "https://rest-prod.immedia-semi.com";

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

        public async Task<BlinkAuthorizationData> AuthorizeAsync()
        {
            var body = new
            {
                unique_id = _userAgent,
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
                _http.DefaultRequestHeaders.Add("token-auth", loginResult.Auth.Token);
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

        public void SetAuthorization(string token, string tier, int clientId, int accountId)
        {
            _clientId = clientId;
            _accountId = accountId;
            string baseUrl = $"https://rest-{tier}.immedia-semi.com";
            _http = new HttpClient()
            {
                BaseAddress = new Uri(baseUrl)
            };
            _http.DefaultRequestHeaders.Add("token-auth", token);
        }

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
            await Task.Delay(1000); // I don't know why, but their server returns empty response without this delay
            var result = await _http.PostAsync(url, null);
            if (!result.IsSuccessStatusCode)
            {
                throw new BlinkClientException("Failed to get videos - " + result.ReasonPhrase);
            }
            var manifestData = await result.Content.ReadFromJsonAsync<ManifestData>()
                ?? throw new BlinkClientException("Failed to get videos - no content");
            url += $"/{manifestData.Id}";
            await Task.Delay(1000); // I don't know why, but their server returns empty response without this delay
            var response = await _http.GetAsync(url);
            var videoResponse = await response.Content.ReadFromJsonAsync<VideoResponse>()
                ?? throw new BlinkClientException("Failed to get videos - no content");
            return videoResponse.Videos;
        }
    }
}
