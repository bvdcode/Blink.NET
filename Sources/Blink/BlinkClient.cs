using System;
using System.Linq;
using System.Net;
using Blink.Models;
using System.Text;
using System.Text.Json;
using System.Net.Http;
using Blink.Exceptions;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

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

        /// <summary>
        /// Gets or sets the hardware identifier used by OAuth v2 flows.
        /// Persist this value between runs to improve refresh-token reliability.
        /// </summary>
        public string HardwareId { get; set; } = Guid.NewGuid().ToString().ToUpperInvariant();

        private string? _tier;
        private string? _email;
        private int? _accountId;
        private string? _password;
        private HttpClient? _http;
        private HttpClient? _oauthHttp;
        private string? _oauthCsrfToken;
        private string? _oauthCodeVerifier;
        private LoginResult? _lastLoginResult;
        private const string Model = "Pixel 8 Pro";
        private const string AppVersion = "48.1";
        private const string OsVersion = "14";
        private const string ClientType = "Android";
        private const string Manufacturer = "Google";
        private const string AppBuild = "ANDROID_29312391";
        private const string OAuthTokenUrl = "https://api.oauth.blink.com/oauth/token";
        private const string OAuthAuthorizeUrl = "https://api.oauth.blink.com/oauth/v2/authorize";
        private const string OAuthSignInUrl = "https://api.oauth.blink.com/oauth/v2/signin";
        private const string OAuth2FaVerifyUrl = "https://api.oauth.blink.com/oauth/v2/2fa/verify";
        private const string OAuthV2ClientId = "ios";
        private const string OAuthScope = "client";
        private const string OAuthRedirectUri = "immedia-blink://applinks.blink.com/signin/callback";
        private const string OAuthWebUserAgent =
            "Mozilla/5.0 (iPhone; CPU iPhone OS 18_7 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/26.1 Mobile/15E148 Safari/604.1";
        private const string OAuthTokenUserAgent = "Blink/2511191620 CFNetwork/3860.200.71 Darwin/25.1.0";
        private static readonly Regex OauthArgsRegex =
            new Regex("<script[^>]*id=[\"']oauth-args[\"'][^>]*>(?<json>.*?)</script>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        //private const string AppVersionFull = "blink-48.1-0b43786c1-hotfix-48.1_fullRelease";
        private readonly string UserAgent = $"Blink/{AppVersion} ({Manufacturer} {Model}; {ClientType} {OsVersion})";

        private enum OAuthLoginStartResult
        {
            Authorized,
            RequiresTwoFactor,
            InvalidCredentials,
            Failed
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

            var oauthRefreshResult = await TryLoginWithOAuthV2RefreshTokenAsync(refreshToken);
            if (oauthRefreshResult.HasValue)
            {
                return oauthRefreshResult.Value;
            }

            var response = await SendLegacyAuthRequestAsync(refreshToken);
            string content = await response.Content.ReadAsStringAsync();
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return false;
            }
            if (!response.IsSuccessStatusCode)
            {
                throw new BlinkClientException($"Failed to authorize with refresh token - {response.StatusCode} ({response.ReasonPhrase}) - {content}");
            }

            var loginResult = JsonSerializer.Deserialize<LoginResult>(content)
                ?? throw new BlinkClientException("Failed to verify pin - no content");
            await ApplySuccessfulLoginAsync(loginResult);
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

            var oauthResult = await TryStartOAuthV2LoginAsync(email, password);
            switch (oauthResult)
            {
                case OAuthLoginStartResult.Authorized:
                case OAuthLoginStartResult.RequiresTwoFactor:
                    return true;
                case OAuthLoginStartResult.InvalidCredentials:
                    return false;
                case OAuthLoginStartResult.Failed:
                default:
                    break;
            }

            var response = await SendLegacyAuthRequestAsync(email, password);
            if (response.StatusCode == HttpStatusCode.PreconditionFailed)
            {
                return true;
            }
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return false;
            }

            string content = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                var loginResult = JsonSerializer.Deserialize<LoginResult>(content)
                    ?? throw new BlinkClientException("Failed to authorize - no content");
                await ApplySuccessfulLoginAsync(loginResult);
                return true;
            }

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

            if (_accountId != null && !string.IsNullOrWhiteSpace(_lastLoginResult?.AccessToken) && string.IsNullOrWhiteSpace(_oauthCsrfToken))
            {
                // Already authorized (possible if login completed without 2FA).
                return true;
            }

            if (!string.IsNullOrWhiteSpace(_oauthCsrfToken) || !string.IsNullOrWhiteSpace(_oauthCodeVerifier))
            {
                return await TryCompleteOAuthV2TwoFactorAsync(code);
            }

            if (string.IsNullOrWhiteSpace(_email) || string.IsNullOrWhiteSpace(_password))
            {
                throw new BlinkClientException("Call TryLoginAsync before verifying pin");
            }

            var response = await SendLegacyAuthRequestAsync(_email, _password, code);
            string content = await response.Content.ReadAsStringAsync();
            if (response.StatusCode == HttpStatusCode.BadRequest && content.Equals("Verification Code is invalid or expired"))
            {
                return false;
            }
            if (!response.IsSuccessStatusCode)
            {
                throw new BlinkClientException($"Failed to verify pin - {response.StatusCode} ({response.ReasonPhrase}) - {content}");
            }

            var loginResult = JsonSerializer.Deserialize<LoginResult>(content)
                ?? throw new BlinkClientException("Failed to verify pin - no content");
            await ApplySuccessfulLoginAsync(loginResult);
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

        private async Task<bool?> TryLoginWithOAuthV2RefreshTokenAsync(string refreshToken)
        {
            try
            {
                using HttpClient http = new HttpClient();
                using var request = new HttpRequestMessage(HttpMethod.Post, OAuthTokenUrl);
                request.Headers.TryAddWithoutValidation("User-Agent", OAuthTokenUserAgent);
                request.Headers.TryAddWithoutValidation("Accept", "*/*");
                request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "grant_type", "refresh_token" },
                    { "refresh_token", refreshToken },
                    { "client_id", OAuthV2ClientId },
                    { "scope", OAuthScope },
                    { "hardware_id", HardwareId },
                });

                var response = await http.SendAsync(request);
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return false;
                }
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                string content = await response.Content.ReadAsStringAsync();
                var loginResult = JsonSerializer.Deserialize<LoginResult>(content)
                    ?? throw new BlinkClientException("Failed to authorize with OAuth v2 refresh token - no content");
                await ApplySuccessfulLoginAsync(loginResult);
                return true;
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        private async Task<OAuthLoginStartResult> TryStartOAuthV2LoginAsync(string email, string password)
        {
            HttpClient oauthHttp = CreateOAuthSession();

            var (codeVerifier, codeChallenge) = GeneratePkcePair();
            bool authorizeRequestSucceeded = await SendOAuthAuthorizeRequestAsync(oauthHttp, codeChallenge);
            if (!authorizeRequestSucceeded)
            {
                ClearPendingOAuthState(disposeSession: true);
                return OAuthLoginStartResult.Failed;
            }

            string? csrfToken = await GetOAuthCsrfTokenAsync(oauthHttp);
            if (string.IsNullOrWhiteSpace(csrfToken))
            {
                ClearPendingOAuthState(disposeSession: true);
                return OAuthLoginStartResult.Failed;
            }

            HttpStatusCode signInStatusCode = await SendOAuthSignInAsync(oauthHttp, email, password, csrfToken);
            if (signInStatusCode == HttpStatusCode.PreconditionFailed)
            {
                _oauthCsrfToken = csrfToken;
                _oauthCodeVerifier = codeVerifier;
                return OAuthLoginStartResult.RequiresTwoFactor;
            }

            if (signInStatusCode == HttpStatusCode.Unauthorized || signInStatusCode == HttpStatusCode.Forbidden || signInStatusCode == HttpStatusCode.BadRequest)
            {
                ClearPendingOAuthState(disposeSession: true);
                return OAuthLoginStartResult.InvalidCredentials;
            }

            if (!IsRedirectStatusCode(signInStatusCode))
            {
                ClearPendingOAuthState(disposeSession: true);
                return OAuthLoginStartResult.Failed;
            }

            string? authorizationCode = await GetOAuthAuthorizationCodeAsync(oauthHttp);
            if (string.IsNullOrWhiteSpace(authorizationCode))
            {
                ClearPendingOAuthState(disposeSession: true);
                return OAuthLoginStartResult.Failed;
            }

            var loginResult = await ExchangeOAuthAuthorizationCodeForTokenAsync(oauthHttp, authorizationCode, codeVerifier);
            if (loginResult == null)
            {
                ClearPendingOAuthState(disposeSession: true);
                return OAuthLoginStartResult.Failed;
            }

            await ApplySuccessfulLoginAsync(loginResult);
            ClearPendingOAuthState(disposeSession: true);
            return OAuthLoginStartResult.Authorized;
        }

        private async Task<bool> TryCompleteOAuthV2TwoFactorAsync(string code)
        {
            if (_oauthHttp == null || string.IsNullOrWhiteSpace(_oauthCsrfToken) || string.IsNullOrWhiteSpace(_oauthCodeVerifier))
            {
                throw new BlinkClientException("Call TryLoginAsync before verifying pin");
            }

            bool verifySucceeded = await VerifyOAuth2FaAsync(_oauthHttp, _oauthCsrfToken, code);
            if (!verifySucceeded)
            {
                return false;
            }

            string? authorizationCode = await GetOAuthAuthorizationCodeAsync(_oauthHttp);
            if (string.IsNullOrWhiteSpace(authorizationCode))
            {
                throw new BlinkClientException("Failed to complete OAuth v2 login after 2FA - missing authorization code");
            }

            var loginResult = await ExchangeOAuthAuthorizationCodeForTokenAsync(_oauthHttp, authorizationCode, _oauthCodeVerifier)
                ?? throw new BlinkClientException("Failed to complete OAuth v2 login after 2FA - unable to exchange code for token");
            await ApplySuccessfulLoginAsync(loginResult);
            ClearPendingOAuthState(disposeSession: true);
            return true;
        }

        private HttpClient CreateOAuthSession()
        {
            ClearPendingOAuthState(disposeSession: true);
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false,
                UseCookies = true,
                CookieContainer = new CookieContainer(),
            };

            _oauthHttp = new HttpClient(handler, disposeHandler: true);
            return _oauthHttp;
        }

        private static (string CodeVerifier, string CodeChallenge) GeneratePkcePair()
        {
            byte[] verifierBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(verifierBytes);
            }

            string codeVerifier = Base64UrlEncode(verifierBytes);
            byte[] challengeBytes;
            using (var sha = SHA256.Create())
            {
                challengeBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
            }

            string codeChallenge = Base64UrlEncode(challengeBytes);
            return (codeVerifier, codeChallenge);
        }

        private static string Base64UrlEncode(byte[] value)
        {
            return Convert.ToBase64String(value)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private async Task<bool> SendOAuthAuthorizeRequestAsync(HttpClient httpClient, string codeChallenge)
        {
            var query = new Dictionary<string, string>
            {
                { "app_brand", "blink" },
                { "app_version", "50.1" },
                { "client_id", OAuthV2ClientId },
                { "code_challenge", codeChallenge },
                { "code_challenge_method", "S256" },
                { "device_brand", "Apple" },
                { "device_model", "iPhone16,1" },
                { "device_os_version", "26.1" },
                { "hardware_id", HardwareId },
                { "redirect_uri", OAuthRedirectUri },
                { "response_type", "code" },
                { "scope", OAuthScope },
            };

            string queryString = string.Join("&", query.Select(pair =>
                $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));

            using var request = new HttpRequestMessage(HttpMethod.Get, $"{OAuthAuthorizeUrl}?{queryString}");
            request.Headers.TryAddWithoutValidation("User-Agent", OAuthWebUserAgent);
            request.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            request.Headers.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.9");

            var response = await httpClient.SendAsync(request);
            return response.IsSuccessStatusCode || IsRedirectStatusCode(response.StatusCode);
        }

        private async Task<string?> GetOAuthCsrfTokenAsync(HttpClient httpClient)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, OAuthSignInUrl);
            request.Headers.TryAddWithoutValidation("User-Agent", OAuthWebUserAgent);
            request.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            request.Headers.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.9");

            var response = await httpClient.SendAsync(request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                return null;
            }

            string html = await response.Content.ReadAsStringAsync();
            return ExtractCsrfToken(html);
        }

        private async Task<HttpStatusCode> SendOAuthSignInAsync(HttpClient httpClient, string email, string password, string csrfToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, OAuthSignInUrl);
            request.Headers.TryAddWithoutValidation("User-Agent", OAuthWebUserAgent);
            request.Headers.TryAddWithoutValidation("Accept", "*/*");
            request.Headers.TryAddWithoutValidation("Origin", "https://api.oauth.blink.com");
            request.Headers.Referrer = new Uri(OAuthSignInUrl);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "username", email },
                { "password", password },
                { "csrf-token", csrfToken },
            });

            var response = await httpClient.SendAsync(request);
            return response.StatusCode;
        }

        private async Task<bool> VerifyOAuth2FaAsync(HttpClient httpClient, string csrfToken, string code)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, OAuth2FaVerifyUrl);
            request.Headers.TryAddWithoutValidation("User-Agent", OAuthWebUserAgent);
            request.Headers.TryAddWithoutValidation("Accept", "*/*");
            request.Headers.TryAddWithoutValidation("Origin", "https://api.oauth.blink.com");
            request.Headers.Referrer = new Uri(OAuthSignInUrl);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "2fa_code", code },
                { "csrf-token", csrfToken },
                { "remember_me", "false" },
            });

            var response = await httpClient.SendAsync(request);
            if (response.StatusCode != HttpStatusCode.Created)
            {
                return false;
            }

            string content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(content))
            {
                return false;
            }

            using var json = JsonDocument.Parse(content);
            if (!json.RootElement.TryGetProperty("status", out JsonElement statusElement))
            {
                return false;
            }

            return string.Equals(statusElement.GetString(), "auth-completed", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<string?> GetOAuthAuthorizationCodeAsync(HttpClient httpClient)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, OAuthAuthorizeUrl);
            request.Headers.TryAddWithoutValidation("User-Agent", OAuthWebUserAgent);
            request.Headers.TryAddWithoutValidation("Accept", "*/*");
            request.Headers.Referrer = new Uri(OAuthSignInUrl);

            var response = await httpClient.SendAsync(request);
            if (!IsRedirectStatusCode(response.StatusCode))
            {
                return null;
            }

            string? location = response.Headers.Location?.ToString();
            if (string.IsNullOrWhiteSpace(location))
            {
                return null;
            }

            return TryGetQueryParameter(location, "code");
        }

        private async Task<LoginResult?> ExchangeOAuthAuthorizationCodeForTokenAsync(HttpClient httpClient, string code, string codeVerifier)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, OAuthTokenUrl);
            request.Headers.TryAddWithoutValidation("User-Agent", OAuthTokenUserAgent);
            request.Headers.TryAddWithoutValidation("Accept", "*/*");
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "app_brand", "blink" },
                { "client_id", OAuthV2ClientId },
                { "code", code },
                { "code_verifier", codeVerifier },
                { "grant_type", "authorization_code" },
                { "hardware_id", HardwareId },
                { "redirect_uri", OAuthRedirectUri },
                { "scope", OAuthScope },
            });

            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            string content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<LoginResult>(content);
        }

        private static string? TryGetQueryParameter(string url, string key)
        {
            int queryIndex = url.IndexOf('?', StringComparison.Ordinal);
            if (queryIndex < 0 || queryIndex + 1 >= url.Length)
            {
                return null;
            }

            string query = url[(queryIndex + 1)..];
            var parameters = query.Split('&', StringSplitOptions.RemoveEmptyEntries);
            foreach (var parameter in parameters)
            {
                var pair = parameter.Split('=', 2);
                if (pair.Length == 0)
                {
                    continue;
                }

                string parameterKey = Uri.UnescapeDataString(pair[0]);
                if (!string.Equals(parameterKey, key, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return pair.Length == 2 ? Uri.UnescapeDataString(pair[1]) : string.Empty;
            }

            return null;
        }

        private static string? ExtractCsrfToken(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return null;
            }

            var match = OauthArgsRegex.Match(html);
            if (!match.Success)
            {
                return null;
            }

            string json = match.Groups["json"].Value.Trim();
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                using var document = JsonDocument.Parse(json);
                return document.RootElement.TryGetProperty("csrf-token", out JsonElement tokenElement)
                    ? tokenElement.GetString()
                    : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private async Task ApplySuccessfulLoginAsync(LoginResult loginResult)
        {
            _lastLoginResult = loginResult;
            _lastLoginResult.ValidUntil = DateTime.UtcNow.AddSeconds(_lastLoginResult.ExpiresInSeconds);

            var tier = await GetTierInfoAsync();
            _accountId = tier.AccountId;
            _tier = tier.Tier;

            ResetApiHttpClient();
            if (!string.IsNullOrWhiteSpace(_lastLoginResult.RefreshToken))
            {
                OnTokenRefreshed?.Invoke(_lastLoginResult.RefreshToken);
            }
        }

        private void ClearPendingOAuthState(bool disposeSession)
        {
            _oauthCsrfToken = null;
            _oauthCodeVerifier = null;

            if (!disposeSession || _oauthHttp == null)
            {
                return;
            }

            _oauthHttp.Dispose();
            _oauthHttp = null;
        }

        private void ResetApiHttpClient()
        {
            if (_http == null)
            {
                return;
            }

            _http.Dispose();
            _http = null;
        }

        private async Task<HttpResponseMessage> SendLegacyAuthRequestAsync(string refreshToken)
        {
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
            return await http.PostAsync(OAuthTokenUrl, formData);
        }

        private async Task<HttpResponseMessage> SendLegacyAuthRequestAsync(string email, string password, string? mfaCode = null)
        {
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
            return await http.PostAsync(OAuthTokenUrl, formData);
        }

        private async Task<HttpClient> GetHttpClientAsync()
        {
            if (string.IsNullOrWhiteSpace(_tier))
            {
                throw new BlinkClientException("You have to login first");
            }
            if (string.IsNullOrWhiteSpace(_lastLoginResult?.AccessToken))
            {
                throw new BlinkClientException("You have to login first");
            }

            if (_lastLoginResult.ValidUntil <= DateTime.UtcNow.AddMinutes(5))
            {
                // Re-authenticate using the refresh token; if a new refresh token is issued, the event will be raised there.
                bool refreshSucceeded = await TryLoginWithRefreshTokenAsync(_lastLoginResult.RefreshToken);
                if (!refreshSucceeded)
                {
                    throw new BlinkClientException("Failed to refresh access token");
                }
            }

            if (_http != null)
            {
                _http.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _lastLoginResult.AccessToken);
                return _http;
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

        private static bool IsRedirectStatusCode(HttpStatusCode statusCode)
        {
            return statusCode == HttpStatusCode.Moved ||
                   statusCode == HttpStatusCode.Redirect ||
                   statusCode == HttpStatusCode.RedirectMethod ||
                   statusCode == HttpStatusCode.TemporaryRedirect ||
                   statusCode == HttpStatusCode.PermanentRedirect;
        }
    }
}
