using Serilog;

namespace Blink.ConsoleTest
{
    public class Program
    {
        public static async Task Main()
        {
            const string email = "";
            const string password = "";
            const string refresh = "";

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateLogger();

            BlinkClient client = new();
            await TestLoginFlowAsync(Log.Logger, client, email, password);
            await TestRefreshFlowAsync(Log.Logger, client, refresh);
        }

        private static async Task TestLoginFlowAsync(ILogger logger, BlinkClient client, string email, string password)
        {
            bool isSuccessfulFirstAuth = await client.TryLoginAsync(email, password);
            if (!isSuccessfulFirstAuth)
            {
                logger.Error("Wrong email or password");
                return;
            }

            // wait for user input for 2FA code
            logger.Information("Enter 2FA code:");

            while (true)
            {
                string code = Console.ReadLine() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(code))
                {
                    logger.Warning("Code cannot be empty. Please enter the 2FA code:");
                    continue;
                }
                bool isSuccessful2FA = await client.TryVerifyPinAsync(code);
                if (isSuccessful2FA)
                {
                    logger.Information("2FA verification successful.");
                    break;
                }
                else
                {
                    logger.Error("Invalid 2FA code. Please try again:");
                }
            }
            var dashboard = await client.GetDashboardAsync();
            logger.Information("Dashboard retrieved. Modules count: {Count}", dashboard.SyncModules.Length);
            logger.Information("Save this refresh token for future use: {RefreshToken}", client.RefreshToken);
        }

        private static async Task TestRefreshFlowAsync(ILogger logger, BlinkClient client, string refresh)
        {
            bool isSuccessfulRefresh = await client.TryLoginWithRefreshTokenAsync(refresh);
            if (!isSuccessfulRefresh)
            {
                logger.Error("Failed to refresh token");
                return;
            }
            logger.Information("Token refresh successful.");
            var dashboard = await client.GetDashboardAsync();
            logger.Information("Dashboard retrieved. Modules count: {Count}", dashboard.SyncModules.Length);
        }
    }
}