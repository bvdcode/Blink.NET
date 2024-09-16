using System.Text.Json.Serialization;

namespace Blink.Models
{
    public class Dashboard
    {
        [JsonPropertyName("sync_modules")]
        public SyncModule[] SyncModules { get; set; } = [];
    }
}