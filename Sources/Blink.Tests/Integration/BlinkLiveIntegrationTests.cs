using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Blink;
using Blink.Models;
using Xunit;
using Xunit.Abstractions;

namespace Blink.Tests.Integration
{
    public class BlinkLiveIntegrationTests
    {
        private readonly ITestOutputHelper _output;

        public BlinkLiveIntegrationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task CanLoginAndReadExtendedApiMethods()
        {
            LiveTestContext context;
            try
            {
                context = await LiveTestContext.CreateAsync(_output);
            }
            catch (InvalidOperationException ex)
            {
                _output.WriteLine(ex.Message);
                return;
            }

            BlinkClient client = context.Client;

            var dashboard = await client.GetDashboardAsync();
            _output.WriteLine($"Dashboard sync modules: {dashboard.SyncModules.Length}");

            var networks = await client.GetNetworksAsync();
            _output.WriteLine($"Networks summary count: {networks.Summary.Count}");

            var notifications = await client.GetNotificationConfigurationAsync();
            _output.WriteLine($"Notification flags count: {notifications.Notifications.Count}");

            var usage = await client.GetCameraUsageAsync();
            _output.WriteLine($"Camera usage networks count: {usage.Networks.Length}");

            Assert.NotNull(dashboard);
            Assert.NotNull(networks);
            Assert.NotNull(notifications);
            Assert.NotNull(usage);

            if (dashboard.SyncModules.Length > 0)
            {
                int networkId = dashboard.SyncModules[0].NetworkId;
                var networkStatus = await client.GetNetworkStatusAsync(networkId);
                var syncModuleInfo = await client.GetSyncModuleInfoAsync(networkId);

                Assert.NotNull(networkStatus);
                Assert.NotNull(syncModuleInfo);

                try
                {
                    var eventsPayload = await client.GetSyncEventsAsync(networkId);
                    Assert.NotNull(eventsPayload);
                }
                catch (Blink.Exceptions.BlinkClientException ex)
                    when (ex.Message.Contains("app update is required", StringComparison.OrdinalIgnoreCase))
                {
                    _output.WriteLine("GetSyncEventsAsync is currently blocked by Blink with 'app update is required'.");
                }
            }

            try
            {
                int cloudCount = await client.GetCloudVideoCountAsync();
                _output.WriteLine($"Cloud video count endpoint result: {cloudCount}");
            }
            catch (Blink.Exceptions.BlinkClientException ex)
                when (ex.Message.Contains("Not Found", StringComparison.OrdinalIgnoreCase))
            {
                _output.WriteLine("GetCloudVideoCountAsync returned Not Found for this account/region.");
            }
        }

        [Fact]
        public async Task CanDownloadLocalVideoWhenAvailable()
        {
            LiveTestContext context;
            try
            {
                context = await LiveTestContext.CreateAsync(_output);
            }
            catch (InvalidOperationException ex)
            {
                _output.WriteLine(ex.Message);
                return;
            }

            BlinkClient client = context.Client;

            var dashboard = await client.GetDashboardAsync();
            if (dashboard.SyncModules.Length == 0)
            {
                _output.WriteLine("No sync modules available for local video test.");
                return;
            }

            var module = dashboard.SyncModules[0];
            var localVideos = await GetLocalVideosWithRetryAsync(client, module, maxAttempts: 4);
            if (localVideos.Count == 0)
            {
                _output.WriteLine("No local videos currently available for local download test.");
                return;
            }

            var selected = localVideos
                .OrderByDescending(video => video.CreatedAt)
                .First();
            byte[] bytes = await client.GetVideoBytesAsync(selected);

            Assert.NotNull(bytes);
            Assert.True(bytes.Length > 0, "Downloaded local video is empty.");

            string outputPath = Path.Combine(GetTestDownloadFolder(), $"local-{selected.Id}.mp4");
            await File.WriteAllBytesAsync(outputPath, bytes);
            Assert.True(File.Exists(outputPath));
            Assert.True(new FileInfo(outputPath).Length > 0);

            _output.WriteLine($"Local video downloaded: {outputPath} ({bytes.Length} bytes)");
        }

        [Fact]
        public async Task CanDownloadCloudVideoWhenAvailable()
        {
            LiveTestContext context;
            try
            {
                context = await LiveTestContext.CreateAsync(_output);
            }
            catch (InvalidOperationException ex)
            {
                _output.WriteLine(ex.Message);
                return;
            }

            BlinkClient client = context.Client;

            var cloudVideos = (await client.GetCloudVideosAsync(maxPages: 10, includeDeleted: true)).ToList();
            int activeCount = cloudVideos.Count(video => !video.IsDeleted);
            _output.WriteLine($"Cloud videos total: {cloudVideos.Count}, active: {activeCount}");

            var selected = cloudVideos
                .Where(video => !video.IsDeleted)
                .OrderByDescending(video => video.CreatedAt)
                .FirstOrDefault();

            if (selected == null)
            {
                _output.WriteLine("No active cloud clips currently available for cloud download test.");
                return;
            }

            byte[] bytes = await client.GetCloudVideoBytesAsync(selected);
            Assert.NotNull(bytes);
            Assert.True(bytes.Length > 0, "Downloaded cloud video is empty.");

            string selectedId = string.IsNullOrWhiteSpace(selected.Id)
                ? DateTime.UtcNow.ToString("yyyyMMddHHmmss")
                : selected.Id;
            string outputPath = Path.Combine(GetTestDownloadFolder(), $"cloud-{selectedId}.mp4");
            await File.WriteAllBytesAsync(outputPath, bytes);
            Assert.True(File.Exists(outputPath));
            Assert.True(new FileInfo(outputPath).Length > 0);

            _output.WriteLine($"Cloud video downloaded: {outputPath} ({bytes.Length} bytes)");
        }

        private static async Task<List<BlinkVideoInfo>> GetLocalVideosWithRetryAsync(BlinkClient client, SyncModule module, int maxAttempts)
        {
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    return (await client.GetVideosFromModuleAsync(module)).ToList();
                }
                catch when (attempt < maxAttempts)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2));
                }
            }

            return (await client.GetVideosFromModuleAsync(module)).ToList();
        }

        private static string GetTestDownloadFolder()
        {
            string path = Path.Combine(Path.GetTempPath(), "Blink.NET.Tests.Downloads");
            Directory.CreateDirectory(path);
            return path;
        }

        private sealed class LiveTestContext
        {
            private readonly TestSecrets _secrets;

            private LiveTestContext(BlinkClient client, TestSecrets secrets)
            {
                Client = client;
                _secrets = secrets;
            }

            public BlinkClient Client { get; }

            public static async Task<LiveTestContext> CreateAsync(ITestOutputHelper output)
            {
                var secrets = TestSecrets.LoadOrThrow();

                if (string.IsNullOrWhiteSpace(secrets.RefreshToken))
                {
                    throw new InvalidOperationException("Refresh token is required in secrets.json for automated integration tests.");
                }

                BlinkClient client = new BlinkClient();
                if (!string.IsNullOrWhiteSpace(secrets.HardwareId))
                {
                    client.HardwareId = secrets.HardwareId;
                }

                client.OnTokenRefreshed += token =>
                {
                    if (!string.IsNullOrWhiteSpace(token) && !string.Equals(secrets.RefreshToken, token, StringComparison.Ordinal))
                    {
                        secrets.RefreshToken = token;
                        secrets.Save();
                    }
                };

                bool authorized;
                try
                {
                    authorized = await client.TryLoginWithRefreshTokenAsync(secrets.RefreshToken);
                }
                catch (HttpRequestException ex)
                {
                    throw new InvalidOperationException($"Blink API is not reachable from current environment: {ex.Message}");
                }

                if (!authorized)
                {
                    throw new InvalidOperationException("Refresh-token authorization failed in integration test.");
                }

                output.WriteLine("Authorized via refresh token.");
                return new LiveTestContext(client, secrets);
            }
        }

        private sealed class TestSecrets
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

            public static TestSecrets LoadOrThrow()
            {
                string[] candidates =
                {
                    Path.Combine(Directory.GetCurrentDirectory(), "Sources", "Blink.ConsoleTest", "secrets.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "Blink.ConsoleTest", "secrets.json"),
                    Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Blink.ConsoleTest", "secrets.json")),
                };

                string? filePath = candidates.FirstOrDefault(File.Exists);
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    throw new InvalidOperationException("Integration secrets file not found. Expected Sources/Blink.ConsoleTest/secrets.json.");
                }

                string json = File.ReadAllText(filePath);
                var secrets = JsonSerializer.Deserialize<TestSecrets>(json)
                    ?? throw new InvalidOperationException("Unable to parse integration secrets file.");
                secrets.FilePath = filePath;
                return secrets;
            }

            public void Save()
            {
                if (string.IsNullOrWhiteSpace(FilePath))
                {
                    return;
                }

                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(FilePath, json);
            }
        }
    }
}
