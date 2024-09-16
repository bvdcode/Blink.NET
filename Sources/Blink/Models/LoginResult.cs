using System.Text.Json.Serialization;

namespace Blink.Models
{
    /// <summary>
    /// Login result
    /// </summary>
    public class LoginResult
    {
        /// <summary>
        /// Account information
        /// </summary>
        [JsonPropertyName("account")]
        public Account Account { get; set; } = null!;

        /// <summary>
        /// Authentification information
        /// </summary>
        [JsonPropertyName("auth")]
        public Auth Auth { get; set; } = null!;

        /// <summary>
        /// Phone information
        /// </summary>
        [JsonPropertyName("phone")]
        public Phone Phone { get; set; } = null!;

        /// <summary>
        /// Lockout time remaining, if lockout is active
        /// </summary>
        [JsonPropertyName("lockout_time_remaining")]
        public int LockoutTimeRemaining { get; set; }

        /// <summary>
        /// Force password reset flag
        /// </summary>
        [JsonPropertyName("force_password_reset")]
        public bool ForcePasswordReset { get; set; }

        /// <summary>
        /// Allow PIN resend seconds
        /// </summary>
        [JsonPropertyName("allow_pin_resend_seconds")]
        public int AllowPinResendSeconds { get; set; }
    }
}