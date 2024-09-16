using System.Text.Json.Serialization;

namespace Blink.Models
{
    public class ManifestData
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("network_id")]
        public int NetworkId { get; set; }
    }
}
