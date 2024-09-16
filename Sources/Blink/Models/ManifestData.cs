using System.Text.Json.Serialization;

namespace Blink.Models
{
    /// <summary>
    /// Manifest data
    /// </summary>
    public class ManifestData
    {
        /// <summary>
        /// Identifier
        /// </summary>
        [JsonPropertyName("id")]
        public long Id { get; set; }

        /// <summary>
        /// Network identifier
        /// </summary>
        [JsonPropertyName("network_id")]
        public int NetworkId { get; set; }
    }
}
