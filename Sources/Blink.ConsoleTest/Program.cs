using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog;

namespace Blink.ConsoleTest
{
    public class Program
    {
        private const string SecretsFileName = "secrets.json";

        public static async Task Main()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateLogger();

            var logger = Log.Logger;
            ConsoleSecrets secrets;
            try
            {
                secrets = await LoadSecretsAsync();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Cannot load {FileName}. Create it and fill in your credentials.", SecretsFileName);
                return;
            }

            BlinkClient client = new();
            if (!string.IsNullOrWhiteSpace(secrets.HardwareId))
            {
                client.HardwareId = secrets.HardwareId;
            }
            else
            {
                secrets.HardwareId = client.HardwareId;
                SaveSecrets(logger, secrets, "Hardware ID initialized in secrets.json.");
            }

            client.OnTokenRefreshed += token => SaveRefreshToken(logger, secrets, token);

            bool isAuthorized = await TryAuthorizeAsync(logger, client, secrets);
            if (!isAuthorized)
            {
                return;
            }

            var dashboard = await client.GetDashboardAsync();
            logger.Information("Dashboard retrieved. Modules count: {Count}", dashboard.SyncModules.Length);
            if (dashboard.SyncModules.Length > 0)
            {
                var videos = await client.GetVideosFromModuleAsync(dashboard.SyncModules[0]);
                logger.Information("Videos listed from first module: {Count}", videos.Count());
            }
        }

        private static async Task<bool> TryAuthorizeAsync(ILogger logger, BlinkClient client, ConsoleSecrets secrets)
        {
            if (!string.IsNullOrWhiteSpace(secrets.RefreshToken))
            {
                logger.Information("Trying refresh-token login.");
                bool isSuccessfulRefresh = await client.TryLoginWithRefreshTokenAsync(secrets.RefreshToken);
                if (isSuccessfulRefresh)
                {
                    logger.Information("Token refresh successful.");
                    return true;
                }

                logger.Warning("Refresh token is invalid. Falling back to email/password login.");
            }

            if (string.IsNullOrWhiteSpace(secrets.Email) || string.IsNullOrWhiteSpace(secrets.Password))
            {
                logger.Error("Email/password are empty in {FileName}.", SecretsFileName);
                return false;
            }

            bool isSuccessfulFirstAuth = await client.TryLoginAsync(secrets.Email, secrets.Password);
            if (!isSuccessfulFirstAuth)
            {
                logger.Error("Wrong email or password");
                return false;
            }

            if (!string.IsNullOrWhiteSpace(client.RefreshToken))
            {
                logger.Information("Login successful.");
                return true;
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
                    return true;
                }
                else
                {
                    logger.Error("Invalid 2FA code. Please try again:");
                }
            }
        }

        private static async Task<ConsoleSecrets> LoadSecretsAsync()
        {
            string filePath = ResolveSecretsPath();
            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Secrets file not found: {filePath}");
            }

            var json = await File.ReadAllTextAsync(filePath);
            var secrets = JsonSerializer.Deserialize<ConsoleSecrets>(json)
                ?? throw new InvalidOperationException("Unable to parse secrets.json");
            secrets.FilePath = filePath;
            return secrets;
        }

        private static string ResolveSecretsPath()
        {
            string[] candidates =
            {
                Path.Combine(Directory.GetCurrentDirectory(), SecretsFileName),
                Path.Combine(Directory.GetCurrentDirectory(), "Sources", "Blink.ConsoleTest", SecretsFileName),
                Path.Combine(AppContext.BaseDirectory, SecretsFileName),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", SecretsFileName)),
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return candidates[0];
        }

        private static void SaveRefreshToken(ILogger logger, ConsoleSecrets secrets, string token)
        {
            if (string.IsNullOrWhiteSpace(token) || string.Equals(secrets.RefreshToken, token, StringComparison.Ordinal))
            {
                return;
            }

            secrets.RefreshToken = token;
            SaveSecrets(logger, secrets, "Refresh token updated in secrets.json.");
        }

        private static void SaveSecrets(ILogger logger, ConsoleSecrets secrets, string message)
        {
            var json = JsonSerializer.Serialize(secrets, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(secrets.FilePath, json);
            logger.Information(message);
        }

        private sealed class ConsoleSecrets
        {
            [JsonPropertyName("email")]
            public string Email { get; set; } = string.Empty;

            [JsonPropertyName("password")]
            public string Password { get; set; } = string.Empty;

            [JsonPropertyName("refreshToken")]
            public string RefreshToken { get; set; } = string.Empty;

            [JsonPropertyName("hardwareId")]
            public string HardwareId { get; set; } = string.Empty;

            [JsonIgnore]
            public string FilePath { get; set; } = string.Empty;
        }
    }
}