using System.Text.Json.Serialization;

namespace Blink.ConsoleTest
{
    public class Secrets
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;

        [JsonPropertyName("tier")]
        public string Tier { get; set; } = string.Empty;

        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;

        [JsonPropertyName("accountId")]
        public int AccountId { get; set; }

        [JsonPropertyName("clientId")]
        public int ClientId { get; set; }
    }
}