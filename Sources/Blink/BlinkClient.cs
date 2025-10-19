using System;
using System.Net;
using Blink.Models;
using System.Net.Http;
using Blink.Exceptions;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Blink
{
    /// <summary>
    /// Blink client class implementing Blink API
    /// </summary>
    public partial class BlinkClient : IBlinkClient
    {
        /// <inheritdoc />
        public event Action<string>? OnTokenRefreshed;
        
        /// <summary>
        /// For some reason, their server returns empty response without this delay.
        /// You can control this delay by setting this property.
        /// Set to 0 to disable delay.
        /// </summary>
        public int GeneralSleepTime { get; set; } = 3500;

        /// <summary>
        /// Gets the refresh token associated with the last login result.
        /// Save this token to reuse it for future logins without needing to provide email and password again.
        /// Null if not logged in.
        /// </summary>
        public string? RefreshToken => _lastLoginResult?.RefreshToken;

        private string? _tier;
        private string? _email;
        private int? _accountId;
        private string? _password;
        private HttpClient? _http;
        private LoginResult? _lastLoginResult;
        private const string Model = "Pixel 8 Pro";
        private const string AppVersion = "48.1";
        private const string OsVersion = "14";
        private const string ClientType = "Android";
        private const string Manufacturer = "Google";
        private const string AppBuild = "ANDROID_29312391";
        //private const string AppVersionFull = "blink-48.1-0b43786c1-hotfix-48.1_fullRelease";
        private readonly string UserAgent = $"Blink/{AppVersion} ({Manufacturer} {Model}; {ClientType} {OsVersion})";

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
            var httpClient = await GetHttpClientAsync();
            var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                throw new BlinkClientException("Failed to get dashboard - " + response.ReasonPhrase);
            }
            return await response.Content.ReadFromJsonAsync<Dashboard>()
                ?? throw new BlinkClientException("Failed to get dashboard - no content");
        }

        /// <summary>
        /// Attempts to log in using the provided refresh token.
        /// </summary>
        /// <param name="refreshToken">The refresh token used to authenticate the user. Cannot be null, empty, or whitespace.</param>
        /// <returns><see langword="true"/> if the login attempt is successful and the refresh token is valid;  <see
        /// langword="false"/> if the refresh token is invalid or unauthorized.</returns>
        /// <exception cref="BlinkClientException">Thrown if <paramref name="refreshToken"/> is null, empty, or whitespace, or if the login attempt fails due
        /// to an unexpected error.</exception>
        public async Task<bool> TryLoginWithRefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                throw new BlinkClientException("Refresh token is required to authorize but not provided");
            }
            var response = await SendAuthRequestAsync(refreshToken);
            string content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new BlinkClientException($"Failed to verify pin - {response.StatusCode} ({response.ReasonPhrase}) - {content}");
            }
            _lastLoginResult = System.Text.Json.JsonSerializer.Deserialize<LoginResult>(content)
                ?? throw new BlinkClientException("Failed to verify pin - no content");
            _lastLoginResult.ValidUntil = DateTime.UtcNow.AddSeconds(_lastLoginResult.ExpiresInSeconds);
            var tier = await GetTierInfoAsync();
            _accountId = tier.AccountId;
            _tier = tier.Tier;
            if (!string.IsNullOrWhiteSpace(_lastLoginResult.RefreshToken))
            {
                OnTokenRefreshed?.Invoke(_lastLoginResult.RefreshToken);
            }
            return true;
        }

        /// <summary>
        /// Attempts to log in using the specified email and password.
        /// </summary>
        /// <remarks>This method sends a login request to the Blink API and evaluates the response to
        /// determine the outcome.  If the response indicates a precondition failure, the method returns <see
        /// langword="true"/>.  If the response indicates unauthorized access, the method returns <see
        /// langword="false"/>.  For all other response statuses, a <see cref="BlinkClientException"/> is
        /// thrown.</remarks>
        /// <param name="email">The email address associated with the account. Cannot be null, empty, or whitespace.</param>
        /// <param name="password">The password for the account. Cannot be null, empty, or whitespace.</param>
        /// <returns><see langword="true"/> if the login attempt requires additional preconditions to be met;  <see
        /// langword="false"/> if the login attempt fails due to invalid credentials.</returns>
        /// <exception cref="BlinkClientException">Thrown if <paramref name="email"/> or <paramref name="password"/> is null, empty, or whitespace,  or if an
        /// unexpected error occurs during the login process.</exception>
        public async Task<bool> TryLoginAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new BlinkClientException("Email is required to authorize but not provided");
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new BlinkClientException("Password is required to authorize but not provided");
            }
            _email = email;
            _password = password;
            var response = await SendAuthRequestAsync(email, password);
            if (response.StatusCode == HttpStatusCode.PreconditionFailed)
            {
                return true;
            }
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return false;
            }
            string content = await response.Content.ReadAsStringAsync();
            throw new BlinkClientException($"Failed to authorize - {response.StatusCode} ({response.ReasonPhrase}) - {content}");
        }

        /// <summary>
        /// Attempts to verify the provided PIN code for authentication.
        /// </summary>
        /// <remarks>Before calling this method, ensure that <see cref="TryLoginAsync"/> has been
        /// successfully called to initialize the email and password.</remarks>
        /// <param name="code">The PIN code to verify. This value cannot be null, empty, or consist only of whitespace.</param>
        /// <returns><see langword="true"/> if the PIN code is successfully verified; <see langword="false"/> if the PIN code is
        /// invalid or expired.</returns>
        /// <exception cref="BlinkClientException">Thrown if <paramref name="code"/> is null, empty, or whitespace. Thrown if <see cref="TryLoginAsync"/> has
        /// not been called prior to this method. Thrown if the server responds with an error other than an invalid or
        /// expired PIN code.</exception>
        public async Task<bool> TryVerifyPinAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new BlinkClientException("Pin code is required to verify pin but not provided");
            }
            if (string.IsNullOrWhiteSpace(_email) || string.IsNullOrWhiteSpace(_password))
            {
                throw new BlinkClientException("Call TryLoginAsync before verifying pin");
            }
            var response = await SendAuthRequestAsync(_email, _password, code);
            string content = await response.Content.ReadAsStringAsync();
            if (response.StatusCode == HttpStatusCode.BadRequest && content.Equals("Verification Code is invalid or expired"))
            {
                return false;
            }
            if (!response.IsSuccessStatusCode)
            {
                throw new BlinkClientException($"Failed to verify pin - {response.StatusCode} ({response.ReasonPhrase}) - {content}");
            }
            _lastLoginResult = System.Text.Json.JsonSerializer.Deserialize<LoginResult>(content)
                ?? throw new BlinkClientException("Failed to verify pin - no content");
            _lastLoginResult.ValidUntil = DateTime.UtcNow.AddSeconds(_lastLoginResult.ExpiresInSeconds);
            var tier = await GetTierInfoAsync();
            _accountId = tier.AccountId;
            _tier = tier.Tier;
            if (!string.IsNullOrWhiteSpace(_lastLoginResult.RefreshToken))
            {
                OnTokenRefreshed?.Invoke(_lastLoginResult.RefreshToken);
            }
            return true;
        }

        private async Task<TierInfo> GetTierInfoAsync()
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _lastLoginResult?.AccessToken);
            const string url = "https://rest-prod.immedia-semi.com/api/v1/users/tier_info";
            return await httpClient.GetFromJsonAsync<TierInfo>(url)
                ?? throw new BlinkClientException("Failed to get tier info - no content");
        }

        private async Task<HttpResponseMessage> SendAuthRequestAsync(string refreshToken)
        {
            const string authUrl = "https://api.oauth.blink.com/oauth/token";
            var formDataDict = new Dictionary<string, string>
            {
                { "refresh_token", refreshToken },
                { "grant_type", "refresh_token" },
                { "client_id", ClientType.ToLower() },
                { "scope", "client" },
            };
            var formData = new FormUrlEncodedContent(formDataDict);
            using HttpClient http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
            return await http.PostAsync(authUrl, formData);
        }

        private async Task<HttpResponseMessage> SendAuthRequestAsync(string email, string password, string? mfaCode = null)
        {
            const string authUrl = "https://api.oauth.blink.com/oauth/token";
            var formDataDict = new Dictionary<string, string>
            {
                { "username", email },
                { "password", password },
                { "grant_type", "password" },
                { "client_id", ClientType.ToLower() },
                { "scope", "client" },
            };
            var formData = new FormUrlEncodedContent(formDataDict);
            using HttpClient http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
            if (!string.IsNullOrWhiteSpace(mfaCode))
            {
                http.DefaultRequestHeaders.TryAddWithoutValidation("2fa-code", mfaCode);
            }
            return await http.PostAsync(authUrl, formData);
        }

        private async Task<HttpClient> GetHttpClientAsync()
        {
            if (string.IsNullOrWhiteSpace(_tier))
            {
                throw new BlinkClientException("You have to login first");
            }
            if (_http != null)
            {
                return _http;
            }
            if (string.IsNullOrWhiteSpace(_lastLoginResult?.AccessToken))
            {
                throw new BlinkClientException("You have to login first");
            }
            if (_lastLoginResult.ValidUntil <= DateTime.UtcNow.AddMinutes(5))
            {
                // Re-authenticate using the refresh token; if a new refresh token is issued, the event will be raised there.
                await TryLoginWithRefreshTokenAsync(_lastLoginResult.RefreshToken);
            }
            _http = new HttpClient()
            {
                BaseAddress = new Uri($"https://rest-{_tier}.immedia-semi.com")
            };
            const string appBuild = AppBuild;
            const string manufacturer = Manufacturer;
            const string androidVersion = OsVersion;
            string userAgent = $"Blink/{AppVersion} ({manufacturer} {Model}; {ClientType} {androidVersion})";
            _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);
            _http.DefaultRequestHeaders.TryAddWithoutValidation("APP_BUILD", appBuild);
            _http.DefaultRequestHeaders.TryAddWithoutValidation("LOCALE", "en_US");
            _http.DefaultRequestHeaders.TryAddWithoutValidation("X-Blink-Time-Zone", "UTC");
            _http.DefaultRequestHeaders.TryAddWithoutValidation("Cache-Control", "no-cache");
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _lastLoginResult.AccessToken);
            return _http;
        }
    }
}
