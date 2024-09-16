using System.Text.Json.Serialization;

namespace Blink.Models
{
    /// <summary>
    /// User information
    /// </summary>
    public class User
    {
        /// <summary>
        /// User identifier
        /// </summary>
        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        /// <summary>
        /// Country
        /// </summary>
        [JsonPropertyName("country")]
        public string Country { get; set; } = string.Empty;
    }
}