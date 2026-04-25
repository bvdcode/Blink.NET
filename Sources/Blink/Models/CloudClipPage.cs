using System;
using System.Text.Json.Serialization;

namespace Blink.Models
{
    internal class CloudClipPage
    {
        [JsonPropertyName("media")]
        public CloudClipInfo[] Clips { get; set; } = Array.Empty<CloudClipInfo>();
    }
}