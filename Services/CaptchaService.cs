using Neelsol.Models;
using System.Text.Json;

namespace Neelsol.Services
{
    public interface ICaptchaService
    {
        Task<(bool IsValid, decimal Score)> VerifyTokenAsync(string token);
    }

    public class CaptchaService : ICaptchaService
    {
        private readonly HttpClient _httpClient;
        private readonly string _secretKey;
        private readonly decimal _minimumScore;
        private readonly ILogger<CaptchaService> _logger;
        private readonly bool _isEnabled;

        public CaptchaService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<CaptchaService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            // Get secret key from environment or configuration
            _secretKey = Environment.GetEnvironmentVariable("RECAPTCHA_SECRET_KEY") ??
                        configuration["Recaptcha:SecretKey"] ?? string.Empty;

            // Get minimum score (default 0.5)
            var minScoreStr = Environment.GetEnvironmentVariable("RECAPTCHA_MIN_SCORE") ??
                            configuration["Recaptcha:MinScore"] ?? "0.5";
            _minimumScore = decimal.TryParse(minScoreStr, out var score) ? score : 0.5m;

            // Check if reCAPTCHA is enabled (disabled if no secret key)
            _isEnabled = !string.IsNullOrEmpty(_secretKey);

            if (!_isEnabled)
            {
                _logger.LogWarning("reCAPTCHA is disabled - no secret key configured");
            }
            else
            {
                _logger.LogInformation($"reCAPTCHA enabled with minimum score: {_minimumScore}");
            }
        }

        public async Task<(bool IsValid, decimal Score)> VerifyTokenAsync(string token)
        {
            // If reCAPTCHA is not configured, skip verification
            if (!_isEnabled)
            {
                _logger.LogWarning("reCAPTCHA verification skipped - service not configured");
                return (true, 1.0m);
            }

            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("reCAPTCHA token is empty");
                return (false, 0.0m);
            }

            try
            {
                var response = await _httpClient.PostAsync(
                    "https://www.google.com/recaptcha/api/siteverify",
                    new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("secret", _secretKey),
                        new KeyValuePair<string, string>("response", token)
                    })
                );

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"reCAPTCHA API returned status code: {response.StatusCode}");
                    return (false, 0.0m);
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<RecaptchaResponse>(jsonResponse);

                if (result == null)
                {
                    _logger.LogError("Failed to deserialize reCAPTCHA response");
                    return (false, 0.0m);
                }

                if (!result.Success)
                {
                    var errors = result.ErrorCodes != null ? string.Join(", ", result.ErrorCodes) : "unknown";
                    _logger.LogWarning($"reCAPTCHA verification failed. Errors: {errors}");
                    return (false, 0.0m);
                }

                var isValid = result.Score >= _minimumScore;
                _logger.LogInformation($"reCAPTCHA score: {result.Score} (threshold: {_minimumScore}, valid: {isValid})");

                return (isValid, result.Score);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying reCAPTCHA token");
                // On error, allow the request to proceed (fail open) to avoid blocking legitimate users
                return (true, 1.0m);
            }
        }
    }
}
