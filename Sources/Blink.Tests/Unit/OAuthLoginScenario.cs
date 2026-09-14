using System.Collections.Specialized;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Xunit;

namespace Blink.Tests.Unit
{
    internal class OAuthLoginScenario : IDisposable
    {
        public const string Email = "account@example.com";
        public const string Password = "test-password";
        public const string Pin = "123456";
        public const string RefreshToken = "test-refresh-token";
        public const string Challenge = """
            {"next_time_in_secs":60,"phone":"+440000000000","tsv_methods":["sms","whatsapp","voice"],"tsv_state":"sms","user_id":1}
            """;
        private const string OAuthBase = "https://api.oauth.blink.com";
        private const string CsrfToken = "test-csrf-token";
        private readonly ScriptedHttpMessageHandler _handler = new();
        private string? _codeChallenge;

        public BlinkClient CreateClient() => new(_handler.CreateClient, _handler.CreateClient);

        public void ExpectSignIn(HttpStatusCode status, string body = "")
        {
            _handler.Expect(HttpMethod.Get, OAuthBase + "/oauth/v2/authorize", HttpStatusCode.Redirect,
                inspect: (request, _) =>
                {
                    NameValueCollection query = HttpUtility.ParseQueryString(request.RequestUri!.Query);
                    _codeChallenge = query["code_challenge"];
                    Assert.False(string.IsNullOrWhiteSpace(_codeChallenge));
                    Assert.Equal("S256", query["code_challenge_method"]);
                }, location: OAuthBase + "/oauth/v2/signin");
            _handler.Expect(HttpMethod.Get, OAuthBase + "/oauth/v2/signin", HttpStatusCode.OK,
                """<script id="oauth-args">{"csrf-token":"test-csrf-token"}</script>""");
            _handler.Expect(HttpMethod.Post, OAuthBase + "/oauth/v2/signin", status, body,
                inspect: (_, content) =>
                {
                    NameValueCollection form = HttpUtility.ParseQueryString(content);
                    Assert.Equal(Email, form["username"]);
                    Assert.Equal(Password, form["password"]);
                    Assert.Equal(CsrfToken, form["csrf-token"]);
                });
        }

        public void ExpectPin(HttpStatusCode status, string body, string pin = Pin)
        {
            _handler.Expect(HttpMethod.Post, OAuthBase + "/oauth/v2/2fa/verify", status, body,
                inspect: (_, content) =>
                {
                    NameValueCollection form = HttpUtility.ParseQueryString(content);
                    Assert.Equal(pin, form["2fa_code"]);
                    Assert.Equal(CsrfToken, form["csrf-token"]);
                });
        }

        public void ExpectAuthorizedSession()
        {
            _handler.Expect(HttpMethod.Get, OAuthBase + "/oauth/v2/authorize", HttpStatusCode.Redirect,
                location: "immedia-blink://applinks.blink.com/signin/callback?code=test-authorization-code");
            _handler.Expect(HttpMethod.Post, OAuthBase + "/oauth/token", HttpStatusCode.OK,
                """{"access_token":"test-access-token","refresh_token":"test-refresh-token","expires_in":3600}""",
                inspect: (_, content) =>
                {
                    NameValueCollection form = HttpUtility.ParseQueryString(content);
                    Assert.Equal("authorization_code", form["grant_type"]);
                    Assert.Equal("test-authorization-code", form["code"]);
                    string verifier = Assert.IsType<string>(form["code_verifier"]);
                    string challenge = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(verifier)))
                        .TrimEnd('=').Replace('+', '-').Replace('/', '_');
                    Assert.Equal(_codeChallenge, challenge);
                });
            _handler.Expect(HttpMethod.Get, "https://rest-prod.immedia-semi.com/api/v1/users/tier_info", HttpStatusCode.OK,
                """{"tier":"test","account_id":1}""", inspect: AssertAccessToken);
            _handler.Expect(HttpMethod.Get, "https://rest-test.immedia-semi.com/api/v3/accounts/1/homescreen", HttpStatusCode.OK,
                """{"sync_modules":[]}""", inspect: AssertAccessToken);
        }

        public void ExpectLegacyRejection()
        {
            _handler.Expect(HttpMethod.Post, OAuthBase + "/oauth/token", HttpStatusCode.Unauthorized,
                inspect: (_, content) => Assert.Equal("password", HttpUtility.ParseQueryString(content)["grant_type"]));
        }

        public void AssertComplete() => _handler.AssertComplete();

        public void Dispose() => _handler.Dispose();

        private static void AssertAccessToken(HttpRequestMessage request, string content)
        {
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            Assert.Equal("test-access-token", request.Headers.Authorization?.Parameter);
        }
    }
}
