using System.Text.Json.Serialization;

namespace Blink.Models
{
    /// <summary>
    /// Phone information
    /// </summary>
    public class Phone
    {
        /// <summary>
        /// Number
        /// </summary>
        [JsonPropertyName("number")]
        public string Number { get; set; } = string.Empty;

        /// <summary>
        /// Number ending, e.g. 1234
        /// </summary>
        [JsonPropertyName("last_4_digits")]
        public string NumberEnding { get; set; } = string.Empty;

        /// <summary>
        /// Country code
        /// </summary>
        [JsonPropertyName("country_calling_code")]
        public string CountryCallingCode { get; set; } = string.Empty;

        /// <summary>
        /// Is number valid flag
        /// </summary>
        [JsonPropertyName("valid")]
        public bool IsValid { get; set; }
    }
}