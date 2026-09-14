using System.Net;
using Blink.Models;
using Xunit;

namespace Blink.Tests.Unit
{
    public class BlinkClientLoginTests
    {
        [Theory]
        [InlineData(HttpStatusCode.Accepted, OAuthLoginScenario.Challenge)]
        [InlineData(HttpStatusCode.PreconditionFailed, "")]
        public async Task TwoFactorChallengePreservesSessionUntilPinCompletes(HttpStatusCode status, string body)
        {
            using OAuthLoginScenario scenario = new();
            scenario.ExpectSignIn(status, body);
            scenario.ExpectPin(HttpStatusCode.Created, """{"status":"auth-completed"}""");
            scenario.ExpectAuthorizedSession();
            BlinkClient client = scenario.CreateClient();
            string? refreshedToken = null;
            client.OnTokenRefreshed += token => refreshedToken = token;

            Assert.True(await client.TryLoginAsync(OAuthLoginScenario.Email, OAuthLoginScenario.Password));
            Assert.Null(client.RefreshToken);
            Assert.Null(refreshedToken);
            Assert.True(await client.TryVerifyPinAsync(OAuthLoginScenario.Pin));
            Assert.Equal(OAuthLoginScenario.RefreshToken, client.RefreshToken);
            Assert.Equal(OAuthLoginScenario.RefreshToken, refreshedToken);
            Dashboard dashboard = await client.GetDashboardAsync();
            Assert.Empty(dashboard.SyncModules);
            scenario.AssertComplete();
        }

        [Theory]
        [InlineData(HttpStatusCode.Accepted)]
        [InlineData(HttpStatusCode.PreconditionFailed)]
        public async Task RejectedPinCanBeRetriedWithSameSession(HttpStatusCode status)
        {
            using OAuthLoginScenario scenario = new();
            scenario.ExpectSignIn(status, OAuthLoginScenario.Challenge);
            scenario.ExpectPin(HttpStatusCode.BadRequest, "", "000000");
            scenario.ExpectPin(HttpStatusCode.Created, """{"status":"auth-completed"}""");
            scenario.ExpectAuthorizedSession();
            BlinkClient client = scenario.CreateClient();

            Assert.True(await client.TryLoginAsync(OAuthLoginScenario.Email, OAuthLoginScenario.Password));
            Assert.False(await client.TryVerifyPinAsync("000000"));
            Assert.Null(client.RefreshToken);
            Assert.True(await client.TryVerifyPinAsync(OAuthLoginScenario.Pin));
            await client.GetDashboardAsync();
            scenario.AssertComplete();
        }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest)]
        [InlineData(HttpStatusCode.Unauthorized)]
        [InlineData(HttpStatusCode.Forbidden)]
        public async Task RejectedCredentialsReturnFalseWithoutStartingAnotherLogin(HttpStatusCode status)
        {
            using OAuthLoginScenario scenario = new();
            scenario.ExpectSignIn(status);
            BlinkClient client = scenario.CreateClient();

            Assert.False(await client.TryLoginAsync(OAuthLoginScenario.Email, OAuthLoginScenario.Password));
            Assert.Null(client.RefreshToken);
            scenario.AssertComplete();
        }

        [Theory]
        [InlineData(HttpStatusCode.OK)]
        [InlineData(HttpStatusCode.TooManyRequests)]
        [InlineData(HttpStatusCode.InternalServerError)]
        public async Task UnexpectedSignInStatusDoesNotBecomeTwoFactorChallenge(HttpStatusCode status)
        {
            using OAuthLoginScenario scenario = new();
            scenario.ExpectSignIn(status, """{"message":"verification service unavailable"}""");
            scenario.ExpectLegacyRejection();
            BlinkClient client = scenario.CreateClient();

            Assert.False(await client.TryLoginAsync(OAuthLoginScenario.Email, OAuthLoginScenario.Password));
            Assert.Null(client.RefreshToken);
            scenario.AssertComplete();
        }

        [Fact]
        public async Task RedirectCompletesLoginWithoutPin()
        {
            using OAuthLoginScenario scenario = new();
            scenario.ExpectSignIn(HttpStatusCode.Redirect);
            scenario.ExpectAuthorizedSession();
            BlinkClient client = scenario.CreateClient();

            Assert.True(await client.TryLoginAsync(OAuthLoginScenario.Email, OAuthLoginScenario.Password));
            Assert.Equal(OAuthLoginScenario.RefreshToken, client.RefreshToken);
            Assert.True(await client.TryVerifyPinAsync(OAuthLoginScenario.Pin));
            await client.GetDashboardAsync();
            scenario.AssertComplete();
        }
    }
}
