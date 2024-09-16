using System.Text.Json.Serialization;

namespace Blink.Models
{
    public class Auth
    {
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;
    }
}