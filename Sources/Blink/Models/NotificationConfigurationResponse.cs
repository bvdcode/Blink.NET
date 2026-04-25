using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Blink.Models
{
    /// <summary>
    /// Notification configuration returned by Blink account notification endpoint.
    /// </summary>
    public class NotificationConfigurationResponse
    {
        /// <summary>
        /// Notification flags keyed by Blink notification name.
        /// </summary>
        [JsonPropertyName("notifications")]
        public Dictionary<string, JsonElement> Notifications { get; set; } = new Dictionary<string, JsonElement>();

        /// <summary>
        /// Additional response fields from Blink API.
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, JsonElement> AdditionalData { get; set; } = new Dictionary<string, JsonElement>();
    }
}
