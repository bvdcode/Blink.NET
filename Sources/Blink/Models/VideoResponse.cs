using System.Text.Json.Serialization;

namespace Blink.Models
{
    public class VideoResponse
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        [JsonPropertyName("manifest_id")]
        public string ManifestId { get; set; } = string.Empty;

        [JsonPropertyName("clips")]
        public BlinkVideoInfo[] Videos { get; set; } = [];
    }
}
