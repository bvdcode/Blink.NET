namespace Blink.Models
{
    /// <summary>
    /// Blink authorization data
    /// </summary>
    public class BlinkAuthorizationData
    {
        /// <summary>
        /// Client ID
        /// </summary>
        public int ClientId { get; set; }

        /// <summary>
        /// Account ID
        /// </summary>
        public int AccountId { get; set; }

        /// <summary>
        /// Server API tier, ex. tier 'u018' will be 'https://rest-u018.immedia-semi.com'
        /// </summary>
        public string Tier { get; set; } = string.Empty;

        /// <summary>
        /// Authorization token
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Convert to JSON string
        /// </summary>
        /// <returns>JSON string</returns>
        public string ToJson()
        {
            return $"{{\"ClientId\":{ClientId},\"AccountId\":{AccountId},\"Tier\":\"{Tier}\",\"Token\":\"{Token}\"}}";
        }
    }
}
