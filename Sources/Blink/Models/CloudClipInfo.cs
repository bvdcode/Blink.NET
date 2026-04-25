using System;
using System.Text.Json.Serialization;

namespace Blink.Models
{
    /// <summary>
    /// Cloud clip metadata returned by Blink cloud media APIs.
    /// </summary>
    public class CloudClipInfo
    {
        /// <summary>
        /// Cloud clip identifier (can be empty in some API responses).
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Name of camera/device that produced the clip.
        /// </summary>
        [JsonPropertyName("device_name")]
        public string CameraName { get; set; } = string.Empty;

        /// <summary>
        /// Cloud media URL for this clip.
        /// </summary>
        [JsonPropertyName("media")]
        public string MediaUrl { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether this clip is marked as deleted on Blink servers.
        /// </summary>
        [JsonPropertyName("deleted")]
        public bool IsDeleted { get; set; }

        /// <summary>
        /// Clip creation timestamp in UTC.
        /// </summary>
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt
        {
            get => _createdAt ?? DateTime.MinValue;
            set
            {
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
    }
}