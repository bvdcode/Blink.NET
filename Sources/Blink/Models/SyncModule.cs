using System.Text.Json.Serialization;

namespace Blink.Models
{
    public class SyncModule
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("network_id")]
        public int NetworkId { get; set; }
    }
}