using System;
using System.Text;
using System.Text.Json;
using Blink.Models;
using System.Net.Http;
using Blink.Exceptions;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Blink
{
    public partial class BlinkClient
    {
        /// <summary>
        /// Retrieves all Blink networks summary.
        /// </summary>
        /// <returns>Network summary response.</returns>
        /// <exception cref="BlinkClientException">Thrown when request fails.</exception>
        public async Task<NetworkSummaryResponse> GetNetworksAsync()
        {
            const string url = "/networks";
            return await GetJsonAsync<NetworkSummaryResponse>(url, "Failed to get networks");
        }

        /// <summary>
        /// Retrieves network status for a specific network.
        /// </summary>
        /// <param name="networkId">Network identifier.</param>
        /// <returns>Network status response.</returns>
        /// <exception cref="BlinkClientException">Thrown when <paramref name="networkId"/> is invalid or request fails.</exception>
        public async Task<NetworkStatusResponse> GetNetworkStatusAsync(int networkId)
        {
            ValidateNetworkId(networkId);
            string url = $"/network/{networkId}";
            return await GetJsonAsync<NetworkStatusResponse>(url, "Failed to get network status");
        }

        /// <summary>
        /// Retrieves camera usage information for all networks.
        /// </summary>
        /// <returns>Camera usage response.</returns>
        /// <exception cref="BlinkClientException">Thrown when request fails.</exception>
        public async Task<CameraUsageResponse> GetCameraUsageAsync()
        {
            const string url = "/api/v1/camera/usage";
            return await GetJsonAsync<CameraUsageResponse>(url, "Failed to get camera usage");
        }

        /// <summary>
        /// Retrieves cameras for a specific network.
        /// </summary>
        /// <param name="networkId">Network identifier.</param>
        /// <returns>Camera payload as key-value JSON dictionary.</returns>
        /// <exception cref="BlinkClientException">Thrown when <paramref name="networkId"/> is invalid or request fails.</exception>
        public async Task<Dictionary<string, JsonElement>> GetCamerasAsync(int networkId)
        {
            ValidateNetworkId(networkId);
            string url = $"/network/{networkId}/cameras";
            return await GetJsonDictionaryAsync(url, "Failed to get cameras");
        }

        /// <summary>
        /// Retrieves camera configuration payload.
        /// </summary>
        /// <param name="networkId">Network identifier.</param>
        /// <param name="cameraId">Camera identifier.</param>
        /// <returns>Camera configuration payload as key-value JSON dictionary.</returns>
        /// <exception cref="BlinkClientException">Thrown when ids are invalid or request fails.</exception>
        public async Task<Dictionary<string, JsonElement>> GetCameraInfoAsync(int networkId, int cameraId)
        {
            ValidateNetworkAndCameraIds(networkId, cameraId);
            string url = $"/network/{networkId}/camera/{cameraId}/config";
            return await GetJsonDictionaryAsync(url, "Failed to get camera info");
        }

        /// <summary>
        /// Retrieves camera sensor payload.
        /// </summary>
        /// <param name="networkId">Network identifier.</param>
        /// <param name="cameraId">Camera identifier.</param>
        /// <returns>Camera sensor payload as key-value JSON dictionary.</returns>
        /// <exception cref="BlinkClientException">Thrown when ids are invalid or request fails.</exception>
        public async Task<Dictionary<string, JsonElement>> GetCameraSensorsAsync(int networkId, int cameraId)
        {
            ValidateNetworkAndCameraIds(networkId, cameraId);
            string url = $"/network/{networkId}/camera/{cameraId}/signals";
            return await GetJsonDictionaryAsync(url, "Failed to get camera sensors");
        }

        /// <summary>
        /// Retrieves sync events for a specific network.
        /// </summary>
        /// <param name="networkId">Network identifier.</param>
        /// <returns>Sync events payload as key-value JSON dictionary.</returns>
        /// <exception cref="BlinkClientException">Thrown when <paramref name="networkId"/> is invalid or request fails.</exception>
        public async Task<Dictionary<string, JsonElement>> GetSyncEventsAsync(int networkId)
        {
            ValidateNetworkId(networkId);
            string url = $"/events/network/{networkId}";
            return await GetJsonDictionaryAsync(url, "Failed to get sync events");
        }

        /// <summary>
        /// Arms a Blink network.
        /// </summary>
        /// <param name="networkId">Network identifier.</param>
        /// <returns><see langword="true"/> if command completed successfully; otherwise <see langword="false"/>.</returns>
        /// <exception cref="BlinkClientException">Thrown when <paramref name="networkId"/> is invalid or request fails.</exception>
        public async Task<bool> ArmSystemAsync(int networkId)
        {
            ValidateNetworkId(networkId);
            int accountId = RequireAccountId();
            string url = $"/api/v1/accounts/{accountId}/networks/{networkId}/state/arm";
            return await ExecuteCommandRequestAsync(url, networkId, null, "Failed to arm network");
        }

        /// <summary>
        /// Disarms a Blink network.
        /// </summary>
        /// <param name="networkId">Network identifier.</param>
        /// <returns><see langword="true"/> if command completed successfully; otherwise <see langword="false"/>.</returns>
        /// <exception cref="BlinkClientException">Thrown when <paramref name="networkId"/> is invalid or request fails.</exception>
        public async Task<bool> DisarmSystemAsync(int networkId)
        {
            ValidateNetworkId(networkId);
            int accountId = RequireAccountId();
            string url = $"/api/v1/accounts/{accountId}/networks/{networkId}/state/disarm";
            return await ExecuteCommandRequestAsync(url, networkId, null, "Failed to disarm network");
        }

        /// <summary>
        /// Retrieves account notification configuration.
        /// </summary>
        /// <returns>Notification configuration payload.</returns>
        /// <exception cref="BlinkClientException">Thrown when request fails.</exception>
        public async Task<NotificationConfigurationResponse> GetNotificationConfigurationAsync()
        {
            int accountId = RequireAccountId();
            string url = $"/api/v1/accounts/{accountId}/notifications/configuration";
            return await GetJsonAsync<NotificationConfigurationResponse>(url, "Failed to get notification configuration");
        }

        /// <summary>
        /// Updates account notification configuration.
        /// </summary>
        /// <param name="notifications">Notification flags to update.</param>
        /// <returns><see langword="true"/> if update request completed successfully; otherwise <see langword="false"/>.</returns>
        /// <exception cref="BlinkClientException">Thrown when <paramref name="notifications"/> is null or request fails.</exception>
        public async Task<bool> SetNotificationConfigurationAsync(IReadOnlyDictionary<string, bool> notifications)
        {
            if (notifications == null)
            {
                throw new BlinkClientException("notifications cannot be null");
            }

            int accountId = RequireAccountId();
            string url = $"/api/v1/accounts/{accountId}/notifications/configuration";
            var payload = JsonContent.Create(new { notifications });
            return await ExecuteCommandRequestAsync(url, fallbackNetworkId: 0, payload, "Failed to set notification configuration");
        }

        /// <summary>
        /// Requests a new image capture from camera.
        /// </summary>
        /// <param name="networkId">Network identifier.</param>
        /// <param name="cameraId">Camera identifier.</param>
        /// <param name="cameraType">Camera product family type.</param>
        /// <returns><see langword="true"/> if request completed successfully; otherwise <see langword="false"/>.</returns>
        /// <exception cref="BlinkClientException">Thrown when ids are invalid or request fails.</exception>
        public async Task<bool> RequestNewImageAsync(int networkId, int cameraId, BlinkCameraType cameraType = BlinkCameraType.Default)
        {
            return await RequestCameraActionAsync(networkId, cameraId, cameraType, CameraAction.Snap);
        }

        /// <summary>
        /// Requests a new video capture from camera.
        /// </summary>
        /// <param name="networkId">Network identifier.</param>
        /// <param name="cameraId">Camera identifier.</param>
        /// <param name="cameraType">Camera product family type.</param>
        /// <returns><see langword="true"/> if request completed successfully; otherwise <see langword="false"/>.</returns>
        /// <exception cref="BlinkClientException">Thrown when ids are invalid or request fails.</exception>
        public async Task<bool> RequestNewVideoAsync(int networkId, int cameraId, BlinkCameraType cameraType = BlinkCameraType.Default)
        {
            return await RequestCameraActionAsync(networkId, cameraId, cameraType, CameraAction.Record);
        }

        /// <summary>
        /// Requests camera live view session.
        /// </summary>
        /// <param name="networkId">Network identifier.</param>
        /// <param name="cameraId">Camera identifier.</param>
        /// <param name="cameraType">Camera product family type.</param>
        /// <returns><see langword="true"/> if request completed successfully; otherwise <see langword="false"/>.</returns>
        /// <exception cref="BlinkClientException">Thrown when ids are invalid or request fails.</exception>
        public async Task<bool> RequestCameraLiveViewAsync(int networkId, int cameraId, BlinkCameraType cameraType = BlinkCameraType.Default)
        {
            return await RequestCameraActionAsync(networkId, cameraId, cameraType, CameraAction.LiveView);
        }

        /// <summary>
        /// Enables motion detection for camera.
        /// </summary>
        /// <param name="networkId">Network identifier.</param>
        /// <param name="cameraId">Camera identifier.</param>
        /// <param name="cameraType">Camera product family type.</param>
        /// <returns><see langword="true"/> if request completed successfully; otherwise <see langword="false"/>.</returns>
        /// <exception cref="BlinkClientException">Thrown when ids are invalid or request fails.</exception>
        public async Task<bool> EnableMotionDetectionAsync(int networkId, int cameraId, BlinkCameraType cameraType = BlinkCameraType.Default)
        {
            return await RequestCameraActionAsync(networkId, cameraId, cameraType, CameraAction.ArmEnable);
        }

        /// <summary>
        /// Disables motion detection for camera.
        /// </summary>
        /// <param name="networkId">Network identifier.</param>
        /// <param name="cameraId">Camera identifier.</param>
        /// <param name="cameraType">Camera product family type.</param>
        /// <returns><see langword="true"/> if request completed successfully; otherwise <see langword="false"/>.</returns>
        /// <exception cref="BlinkClientException">Thrown when ids are invalid or request fails.</exception>
        public async Task<bool> DisableMotionDetectionAsync(int networkId, int cameraId, BlinkCameraType cameraType = BlinkCameraType.Default)
        {
            return await RequestCameraActionAsync(networkId, cameraId, cameraType, CameraAction.ArmDisable);
        }

        private enum CameraAction
        {
            ArmEnable,
            ArmDisable,
            Record,
            Snap,
            LiveView,
        }

        private async Task<bool> RequestCameraActionAsync(int networkId, int cameraId, BlinkCameraType cameraType, CameraAction action)
        {
            ValidateNetworkAndCameraIds(networkId, cameraId);
            int accountId = RequireAccountId();

            var (url, content) = BuildCameraActionRequest(accountId, networkId, cameraId, cameraType, action);
            return await ExecuteCommandRequestAsync(url, networkId, content, "Failed to request camera action");
        }

        private static (string Url, HttpContent? Content) BuildCameraActionRequest(int accountId, int networkId, int cameraId, BlinkCameraType cameraType, CameraAction action)
        {
            return cameraType switch
            {
                BlinkCameraType.Default => BuildDefaultCameraActionRequest(accountId, networkId, cameraId, action),
                BlinkCameraType.Mini => BuildMiniCameraActionRequest(accountId, networkId, cameraId, action),
                BlinkCameraType.Doorbell => BuildDoorbellCameraActionRequest(accountId, networkId, cameraId, action),
                _ => throw new BlinkClientException($"Unsupported camera type: {cameraType}"),
            };
        }

        private static (string Url, HttpContent? Content) BuildDefaultCameraActionRequest(int accountId, int networkId, int cameraId, CameraAction action)
        {
            return action switch
            {
                CameraAction.ArmEnable => ($"/network/{networkId}/camera/{cameraId}/enable", null),
                CameraAction.ArmDisable => ($"/network/{networkId}/camera/{cameraId}/disable", null),
                CameraAction.Record => ($"/network/{networkId}/camera/{cameraId}/clip", null),
                CameraAction.Snap => ($"/network/{networkId}/camera/{cameraId}/thumbnail", null),
                CameraAction.LiveView => ($"/api/v5/accounts/{accountId}/networks/{networkId}/cameras/{cameraId}/liveview", JsonContent.Create(new { intent = "liveview" })),
                _ => throw new BlinkClientException($"Unsupported camera action: {action}"),
            };
        }

        private static (string Url, HttpContent? Content) BuildMiniCameraActionRequest(int accountId, int networkId, int cameraId, CameraAction action)
        {
            string cameraBase = $"/api/v1/accounts/{accountId}/networks/{networkId}/owls/{cameraId}";
            return action switch
            {
                CameraAction.ArmEnable => ($"{cameraBase}/config", JsonContent.Create(new { enabled = true })),
                CameraAction.ArmDisable => ($"{cameraBase}/config", JsonContent.Create(new { enabled = false })),
                CameraAction.Record => ($"{cameraBase}/clip", null),
                CameraAction.Snap => ($"{cameraBase}/thumbnail", null),
                CameraAction.LiveView => ($"{cameraBase}/liveview", JsonContent.Create(new { intent = "liveview" })),
                _ => throw new BlinkClientException($"Unsupported camera action: {action}"),
            };
        }

        private static (string Url, HttpContent? Content) BuildDoorbellCameraActionRequest(int accountId, int networkId, int cameraId, CameraAction action)
        {
            string cameraBase = $"/api/v1/accounts/{accountId}/networks/{networkId}/doorbells/{cameraId}";
            return action switch
            {
                CameraAction.ArmEnable => ($"{cameraBase}/enable", null),
                CameraAction.ArmDisable => ($"{cameraBase}/disable", null),
                CameraAction.Record => ($"{cameraBase}/clip", null),
                CameraAction.Snap => ($"{cameraBase}/thumbnail", null),
                CameraAction.LiveView => ($"{cameraBase}/liveview", JsonContent.Create(new { intent = "liveview" })),
                _ => throw new BlinkClientException($"Unsupported camera action: {action}"),
            };
        }

        private async Task<T> GetJsonAsync<T>(string url, string errorPrefix)
        {
            var httpClient = await GetHttpClientAsync();
            var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                throw new BlinkClientException($"{errorPrefix} - {response.ReasonPhrase} - {error}");
            }

            return await response.Content.ReadFromJsonAsync<T>()
                ?? throw new BlinkClientException($"{errorPrefix} - no content");
        }

        private async Task<Dictionary<string, JsonElement>> GetJsonDictionaryAsync(string url, string errorPrefix)
        {
            var result = await GetJsonAsync<Dictionary<string, JsonElement>>(url, errorPrefix);
            return result;
        }

        private async Task<bool> ExecuteCommandRequestAsync(string url, int fallbackNetworkId, HttpContent? content, string errorPrefix)
        {
            var httpClient = await GetHttpClientAsync();
            await Task.Delay(GeneralSleepTime);

            var response = await httpClient.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                throw new BlinkClientException($"{errorPrefix} - {response.ReasonPhrase} - {error}");
            }

            CommandRequestResult? command = await TryReadCommandRequestAsync(response);
            if (command == null || command.Id <= 0)
            {
                return true;
            }

            int networkId = command.NetworkId > 0 ? command.NetworkId : fallbackNetworkId;
            if (networkId <= 0)
            {
                return true;
            }

            return await WaitForCommandCompletionAsync(httpClient, networkId, command.Id);
        }

        private static async Task<CommandRequestResult?> TryReadCommandRequestAsync(HttpResponseMessage response)
        {
            try
            {
                return await response.Content.ReadFromJsonAsync<CommandRequestResult>();
            }
            catch (JsonException)
            {
                return null;
            }
            catch (NotSupportedException)
            {
                return null;
            }
        }

        private int RequireAccountId()
        {
            if (_accountId == null)
            {
                throw new BlinkClientException("Not authorized");
            }

            return _accountId.Value;
        }

        private static void ValidateNetworkId(int networkId)
        {
            if (networkId <= 0)
            {
                throw new BlinkClientException("networkId must be greater than 0");
            }
        }

        private static void ValidateNetworkAndCameraIds(int networkId, int cameraId)
        {
            ValidateNetworkId(networkId);
            if (cameraId <= 0)
            {
                throw new BlinkClientException("cameraId must be greater than 0");
            }
        }
    }
}
