using System.Text.Json.Serialization;

namespace Blink.Models
{
    internal class CommandStatus
    {
        [JsonPropertyName("status_code")]
        public int StatusCode { get; set; }

        [JsonPropertyName("complete")]
        public bool Complete { get; set; }
    }
}
