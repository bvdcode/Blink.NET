using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Blink.Models
{
    internal class CommandRequestResult
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("network_id")]
        public int NetworkId { get; set; }

        [JsonExtensionData]
        public IDictionary<string, JsonElement> AdditionalData { get; set; } = new Dictionary<string, JsonElement>();
    }
}
