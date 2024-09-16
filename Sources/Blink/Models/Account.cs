using System.Text.Json.Serialization;

namespace Blink.Models
{
    public class Account
    {
        [JsonPropertyName("account_id")]
        public int AccountId { get; set; }

        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        [JsonPropertyName("client_id")]
        public int ClientId { get; set; }

        [JsonPropertyName("client_trusted")]
        public bool IsClientTrusted { get; set; }

        [JsonPropertyName("new_account")]
        public bool IsNewAccount { get; set; }

        [JsonPropertyName("tier")]
        public string Tier { get; set; } = string.Empty;

        [JsonPropertyName("region")]
        public string Region { get; set; } = string.Empty;

        [JsonPropertyName("account_verification_required")]
        public bool IsAccountVerificationRequired { get; set; }

        [JsonPropertyName("phone_verification_required")]
        public bool IsPhoneVerificationRequired { get; set; }

        [JsonPropertyName("client_verification_required")]
        public bool IsClientVerificationRequired { get; set; }

        [JsonPropertyName("require_trust_client_device")]
        public bool IsTrustClientDeviceRequired { get; set; }

        [JsonPropertyName("country_required")]
        public bool IsCountryRequired { get; set; }

        [JsonPropertyName("verification_channel")]
        public string VerificationChannel { get; set; } = string.Empty;

        [JsonPropertyName("user")]
        public User User { get; set; } = null!;

        [JsonPropertyName("amazon_account_linked")]
        public bool IsAmazonAccountLinked { get; set; }

        [JsonPropertyName("braze_external_id")]
        public string BrazeExternalId { get; set; } = string.Empty;
    }
}