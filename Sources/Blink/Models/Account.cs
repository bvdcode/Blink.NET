using System.Text.Json.Serialization;

namespace Blink.Models
{
    /// <summary>
    /// Account information
    /// </summary>
    public class Account
    {
        /// <summary>
        /// Account identifier
        /// </summary>
        [JsonPropertyName("account_id")]
        public int AccountId { get; set; }

        /// <summary>
        /// User identifier
        /// </summary>
        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        /// <summary>
        /// Client identifier
        /// </summary>
        [JsonPropertyName("client_id")]
        public int ClientId { get; set; }

        /// <summary>
        /// Is client trusted flag
        /// </summary>
        [JsonPropertyName("client_trusted")]
        public bool IsClientTrusted { get; set; }

        /// <summary>
        /// Is new account flag
        /// </summary>
        [JsonPropertyName("new_account")]
        public bool IsNewAccount { get; set; }

        /// <summary>
        /// Tier, ex. tier 'u018' will be 'https://rest-u018.immedia-semi.com'
        /// </summary>
        [JsonPropertyName("tier")]
        public string Tier { get; set; } = string.Empty;

        /// <summary>
        /// Region, ex. 'us', 'eu'
        /// </summary>
        [JsonPropertyName("region")]
        public string Region { get; set; } = string.Empty;

        /// <summary>
        /// Account verification required flag
        /// </summary>
        [JsonPropertyName("account_verification_required")]
        public bool IsAccountVerificationRequired { get; set; }

        /// <summary>
        /// Phone verification required flag
        /// </summary>
        [JsonPropertyName("phone_verification_required")]
        public bool IsPhoneVerificationRequired { get; set; }

        /// <summary>
        /// Client verification required flag
        /// </summary>
        [JsonPropertyName("client_verification_required")]
        public bool IsClientVerificationRequired { get; set; }

        /// <summary>
        /// Trust client device required flag
        /// </summary>
        [JsonPropertyName("require_trust_client_device")]
        public bool IsTrustClientDeviceRequired { get; set; }

        /// <summary>
        /// Country required flag
        /// </summary>
        [JsonPropertyName("country_required")]
        public bool IsCountryRequired { get; set; }

        /// <summary>
        /// Verification channel
        /// </summary>
        [JsonPropertyName("verification_channel")]
        public string VerificationChannel { get; set; } = string.Empty;

        /// <summary>
        /// User information
        /// </summary>
        [JsonPropertyName("user")]
        public User User { get; set; } = null!;

        /// <summary>
        /// Amazon account linked flag
        /// </summary>
        [JsonPropertyName("amazon_account_linked")]
        public bool IsAmazonAccountLinked { get; set; }

        /// <summary>
        /// Braze external identifier
        /// </summary>
        [JsonPropertyName("braze_external_id")]
        public string BrazeExternalId { get; set; } = string.Empty;
    }
}