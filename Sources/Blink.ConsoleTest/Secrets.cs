using System.Text.Json.Serialization;

namespace Blink.ConsoleTest
{
    public class Secrets
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;
    }
}