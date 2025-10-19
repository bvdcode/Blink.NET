using System;
using System.Text.Json.Serialization;

namespace Blink.Models
{
    internal class LoginResult
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = null!;

        [JsonPropertyName("expires_in")]
        public int ExpiresInSeconds { get; set; }

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = null!;

        [JsonPropertyName("scope")]
        public string Scope { get; set; } = null!;

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = null!;

        [JsonIgnore]
        public DateTime ValidUntil { get; set; }
    }
}
