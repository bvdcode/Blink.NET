﻿using System;
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
        /// Video timestamp
        /// </summary>
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
