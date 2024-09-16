using System;
using System.Text.Json.Serialization;

namespace Blink.Models
{
    /// <summary>
    /// Dashboard
    /// </summary>
    public class Dashboard
    {
        /// <summary>
        /// Sync modules of the Blink system
        /// </summary>
        [JsonPropertyName("sync_modules")]
        public SyncModule[] SyncModules { get; set; } = Array.Empty<SyncModule>();
    }
}