using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Blink.Models
{
    /// <summary>
    /// Response wrapper for network status endpoint.
    /// </summary>
    public class NetworkStatusResponse
    {
        /// <summary>
        /// Network status payload.
        /// </summary>
        [JsonPropertyName("network")]
        public NetworkStatusInfo Network { get; set; } = new NetworkStatusInfo();

        /// <summary>
        /// Additional response fields from Blink API.
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, JsonElement> AdditionalData { get; set; } = new Dictionary<string, JsonElement>();
    }

    /// <summary>
    /// Network status details.
    /// </summary>
    public class NetworkStatusInfo
    {
        /// <summary>
        /// Network name.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether this network is armed.
        /// </summary>
        [JsonPropertyName("armed")]
        public bool? IsArmed { get; set; }

        /// <summary>
        /// Additional network fields from Blink API.
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, JsonElement> AdditionalData { get; set; } = new Dictionary<string, JsonElement>();
    }
}
