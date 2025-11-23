using System.Text.Json.Serialization;

namespace Neelsol.Models
{
    /// <summary>
    /// Response model from Google reCAPTCHA verification API
    /// </summary>
    public class RecaptchaResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("score")]
        public decimal Score { get; set; }

        [JsonPropertyName("action")]
        public string Action { get; set; } = string.Empty;

        [JsonPropertyName("challenge_ts")]
        public string ChallengeTs { get; set; } = string.Empty;

        [JsonPropertyName("hostname")]
        public string Hostname { get; set; } = string.Empty;

        [JsonPropertyName("error-codes")]
        public string[]? ErrorCodes { get; set; }
    }
}
