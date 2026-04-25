using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Blink.Models
{
    /// <summary>
    /// Camera usage payload returned by Blink camera usage endpoint.
    /// </summary>
    public class CameraUsageResponse
    {
        /// <summary>
        /// Networks with attached camera usage information.
        /// </summary>
        [JsonPropertyName("networks")]
        public CameraUsageNetwork[] Networks { get; set; } = Array.Empty<CameraUsageNetwork>();

        /// <summary>
        /// Additional response fields from Blink API.
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, JsonElement> AdditionalData { get; set; } = new Dictionary<string, JsonElement>();
    }

    /// <summary>
    /// Camera usage for a single Blink network.
    /// </summary>
    public class CameraUsageNetwork
    {
        /// <summary>
        /// Network identifier.
        /// </summary>
        [JsonPropertyName("network_id")]
        public int NetworkId { get; set; }

        /// <summary>
        /// Cameras attached to this network.
        /// </summary>
        [JsonPropertyName("cameras")]
        public CameraUsageCamera[] Cameras { get; set; } = Array.Empty<CameraUsageCamera>();

        /// <summary>
        /// Additional network usage fields from Blink API.
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, JsonElement> AdditionalData { get; set; } = new Dictionary<string, JsonElement>();
    }

    /// <summary>
    /// Camera usage item.
    /// </summary>
    public class CameraUsageCamera
    {
        /// <summary>
        /// Camera identifier.
        /// </summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }

        /// <summary>
        /// Camera name.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Additional camera usage fields from Blink API.
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, JsonElement> AdditionalData { get; set; } = new Dictionary<string, JsonElement>();
    }
}
