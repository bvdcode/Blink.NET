using System.Text.Json.Serialization;

namespace Blink.Models
{
    internal class TierInfo
    {
        [JsonPropertyName("tier")]
        public string Tier { get; set; } = null!;

        [JsonPropertyName("account_id")]
        public int AccountId { get; set; }

        [JsonPropertyName("tulsa_id")]
        public int TulsaId { get; set; }
    }
}
