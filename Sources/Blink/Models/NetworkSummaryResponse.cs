using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Blink.Models
{
    /// <summary>
    /// Network summary response returned by the Blink networks endpoint.
    /// </summary>
    public class NetworkSummaryResponse
    {
        /// <summary>
        /// Network summary keyed by network identifier.
        /// </summary>
        [JsonPropertyName("summary")]
        public Dictionary<string, NetworkSummaryItem> Summary { get; set; } = new Dictionary<string, NetworkSummaryItem>();
    }

    /// <summary>
    /// Summary item for a single Blink network.
    /// </summary>
    public class NetworkSummaryItem
    {
        /// <summary>
        /// Network display name.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether this network is onboarded.
        /// </summary>
        [JsonPropertyName("onboarded")]
        public bool IsOnboarded { get; set; }

        /// <summary>
        /// Additional response fields from Blink API.
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, JsonElement> AdditionalData { get; set; } = new Dictionary<string, JsonElement>();
    }
}
