using System;
using System.Text.Json.Serialization;

namespace Blink.Models
{
    /// <summary>
    /// Blink video info
    /// </summary>
    public class BlinkVideoInfo
    {
        /// <summary>
        /// Video ID
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Video file size
        /// </summary>
        [JsonPropertyName("size")]
        public string Size { get; set; } = string.Empty;

        /// <summary>
        /// Camera name
        /// </summary>
        [JsonPropertyName("camera_name")]
        public string CameraName { get; set; } = string.Empty;

        /// <summary>
        /// Video timestamp in UTC
        /// </summary>
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt
        {
            get => _createdAt ?? DateTime.MinValue;
            set
            {
                // their format is 2024-09-23T01:01:43+00:00 and it's UTC time,
                // but JSON parsing as local time, so we need to convert it to UTC
                if (value.Kind == DateTimeKind.Local)
                {
                    _createdAt = value.ToUniversalTime();
                }
                else
                {
                    _createdAt = value;
                }
            }
        }

        private DateTime? _createdAt;

        /// <summary>
        /// Network ID
        /// </summary>
        [JsonIgnore]
        public int NetworkId { get; set; }

        /// <summary>
        /// Sync module ID
        /// </summary>
        [JsonIgnore]
        public int ModuleId { get; set; }

        /// <summary>
        /// Video Manifest ID
        /// </summary>
        [JsonIgnore]
        public string ManifestId { get; set; } = string.Empty;
    }
}
