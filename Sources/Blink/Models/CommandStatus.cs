using System.Text.Json.Serialization;

namespace Blink.Models
{
    /// <summary>
    /// Status payload for Blink command polling endpoint.
    /// </summary>
    public class CommandStatus
    {
        [JsonPropertyName("status_code")]
        public int StatusCode { get; set; }

        [JsonPropertyName("complete")]
        public bool Complete { get; set; }
    }
}
