using System.Text.Json.Serialization;

namespace Blink.Models
{
    public class User
    {
        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; } = string.Empty;
    }
}