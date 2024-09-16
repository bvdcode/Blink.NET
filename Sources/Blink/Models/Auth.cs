using System.Text.Json.Serialization;

namespace Blink.Models
{
    /// <summary>
    /// Authentification class
    /// </summary>
    public class Auth
    {
        /// <summary>
        /// Authentification token
        /// </summary>
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;
    }
}