using System.Text.Json.Serialization;

namespace Blink.Models
{
    /// <summary>
    /// Sync module information
    /// </summary>
    public class SyncModule
    {
        /// <summary>
        /// Identifier
        /// </summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }

        /// <summary>
        /// Network identifier
        /// </summary>
        [JsonPropertyName("network_id")]
        public int NetworkId { get; set; }
    }
}