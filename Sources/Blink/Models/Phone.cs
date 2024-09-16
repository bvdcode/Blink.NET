using System.Text.Json.Serialization;

namespace Blink.Models
{
    public class Phone
    {
        [JsonPropertyName("number")]
        public string Number { get; set; } = string.Empty;

        [JsonPropertyName("last_4_digits")]
        public string NumberEnding { get; set; } = string.Empty;

        [JsonPropertyName("country_calling_code")]
        public string CountryCallingCode { get; set; } = string.Empty;

        [JsonPropertyName("valid")]
        public bool IsValid { get; set; }
    }
}