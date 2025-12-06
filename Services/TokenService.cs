using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Codenex.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Codenex.Services
{
    public class TokenService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<TokenService> _logger;

        public TokenService(IOptions<JwtSettings> jwtSettings, ILogger<TokenService> logger)
        {
            _jwtSettings = jwtSettings.Value;
            _logger = logger;
            
            // Validate that JWT key is configured - no fallback allowed
            if (string.IsNullOrEmpty(_jwtSettings.Key))
            {
                throw new InvalidOperationException(
                    "JWT Key is not configured. Set JWT_KEY environment variable or configure Jwt:Key in appsettings.");
            }
            
            if (_jwtSettings.Key.Length < 32)
            {
                throw new InvalidOperationException(
                    "JWT Key must be at least 32 characters long for security.");
            }
        }

        public string CreateToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Email, user.Email ?? string.Empty),
                new(ClaimTypes.GivenName, user.FirstName),
                new(ClaimTypes.Surname, user.LastName),
                new(ClaimTypes.Role, user.Role),
                new("firstName", user.FirstName),
                new("lastName", user.LastName),
                new("role", user.Role),
                new("emailVerified", user.EmailConfirmed.ToString().ToLower())
            };

            // Use configured expiry hours from JwtSettings
            var expiresAt = DateTime.UtcNow.AddHours(_jwtSettings.ExpiryHours);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));

                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidIssuer = _jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                return principal;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token validation failed");
                return null;
            }
        }
        
        /// <summary>
        /// Get the configured token expiry time in hours
        /// </summary>
        public int GetExpiryHours() => _jwtSettings.ExpiryHours;
    }
}
