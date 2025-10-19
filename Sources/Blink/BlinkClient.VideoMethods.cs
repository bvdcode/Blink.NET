using System;
using System.Linq;
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
        /// Get videos from specified module. Get modules from <see cref="GetDashboardAsync"/> method - <see cref="Dashboard.SyncModules"/>.
        /// </summary>
        /// <returns>Collection of <see cref="BlinkVideoInfo"/> objects with video data</returns>
        /// <exception cref="BlinkClientException">Thrown when not authorized</exception>
        /// <exception cref="BlinkClientException">Thrown when failed to get videos</exception>
        public async Task<IEnumerable<BlinkVideoInfo>> GetVideosFromModuleAsync(SyncModule module)
        {
            string url = $"/api/v1/accounts/{_accountId}/networks/{module.NetworkId}/" +
                $"sync_modules/{module.Id}/local_storage/manifest/request";
            await Task.Delay(GeneralSleepTime); // I don't know why, but their server returns empty response without this delay
            var httpClient = await GetHttpClientAsync();
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
        /// Get videos from single module (the first one in the dashboard). Throws exception if more than one module found.
        /// </summary>
        /// <returns>Collection of <see cref="BlinkVideoInfo"/> objects with video data</returns>
        /// <exception cref="BlinkClientException">Thrown when not authorized</exception>
        /// <exception cref="BlinkClientException">Thrown when failed to get videos</exception>
        /// <exception cref="BlinkClientException">Thrown when more than one module found</exception>
        public async Task<IEnumerable<BlinkVideoInfo>> GetVideosFromSingleModuleAsync()
        {
            if (_accountId == null)
            {
                throw new BlinkClientException("Not authorized");
            }
            var dashboard = await GetDashboardAsync();
            if (dashboard.SyncModules.Length > 1)
            {
                throw new BlinkClientException("More than one sync module found. Use GetVideosAsync() instead.");
            }
            var module = dashboard.SyncModules.SingleOrDefault()
                ?? throw new BlinkClientException("No sync modules found");
            return await GetVideosFromModuleAsync(module);
        }

        /// <summary>
        /// Get video file as byte array.
        /// </summary>
        /// <param name="video"><see cref="BlinkVideoInfo"/> object with video data</param>
        /// <param name="tryCount">Number of tries to get video</param>
        /// <returns>Video as byte array</returns>
        /// <exception cref="BlinkClientException">Thrown when not authorized</exception>
        /// <exception cref="BlinkClientException">Thrown when video data is not valid. Please create an issue if you see this error.</exception>
        public async Task<byte[]> GetVideoBytesAsync(BlinkVideoInfo video, int tryCount = 3)
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
            var httpClient = await GetHttpClientAsync();
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

            // DEBUG: try to read response content for better error message if not video/mp4
            string responseContent = "No response";
            if (response != null)
            {
                try
                {
                    responseContent = await response.Content.ReadAsStringAsync();
                }
                catch (Exception ex)
                {
                    responseContent = "Failed to read response content: " + ex.Message;
                }
            }
            throw new BlinkClientException($"Failed to get video {video.Id}, contentType {contentType} - {response?.ReasonPhrase ?? "Unknown Error"}. " +
                $"Please create an issue if you see this error. Content: " + responseContent);
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
            var httpClient = await GetHttpClientAsync();
            await Task.Delay(GeneralSleepTime);
            var result = await httpClient.PostAsync(url, null);
            if (!result.IsSuccessStatusCode)
            {
                string error = await result.Content.ReadAsStringAsync();
                throw new BlinkClientException($"Failed to delete video {video.Id} - {result.ReasonPhrase} - {error}");
            }
            result.EnsureSuccessStatusCode();
        }
    }
}
