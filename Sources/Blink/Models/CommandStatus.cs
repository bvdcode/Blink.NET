using System.Text.Json.Serialization;

namespace Blink.Models
{
    /// <summary>
    /// Status payload for Blink command polling endpoint.
    /// </summary>
    public class CommandStatus
    {
        /// <summary>
        /// Blink command status code where 908 means command accepted/in progress.
        /// </summary>
        [JsonPropertyName("status_code")]
        public int StatusCode { get; set; }

        /// <summary>
        /// Indicates whether Blink command processing has completed.
        /// </summary>
        [JsonPropertyName("complete")]
        public bool Complete { get; set; }
    }
}
