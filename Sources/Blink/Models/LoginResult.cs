using System.Text.Json.Serialization;

namespace Blink.Models
{
    public class LoginResult
    {
        [JsonPropertyName("account")]
        public Account Account { get; set; } = null!;

        [JsonPropertyName("auth")]
        public Auth Auth { get; set; } = null!;

        [JsonPropertyName("phone")]
        public Phone Phone { get; set; } = null!;

        [JsonPropertyName("lockout_time_remaining")]
        public int LockoutTimeRemaining { get; set; }

        [JsonPropertyName("force_password_reset")]
        public bool ForcePasswordReset { get; set; }

        [JsonPropertyName("allow_pin_resend_seconds")]
        public int AllowPinResendSeconds { get; set; }
    }
}