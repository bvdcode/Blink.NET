using System;
using System.Text.Json.Serialization;

namespace Blink.Models
{
    /// <summary>
    /// Video response
    /// </summary>
    public class VideoResponse
    {
        /// <summary>
        /// Version
        /// </summary>
        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Manifest ID
        /// </summary>
        [JsonPropertyName("manifest_id")]
        public string ManifestId { get; set; } = string.Empty;

        /// <summary>
        /// Videos
        /// </summary>
        [JsonPropertyName("clips")]
        public BlinkVideoInfo[] Videos { get; set; } = Array.Empty<BlinkVideoInfo>();
    }
}
