using Microsoft.IdentityModel.Tokens;
using PortfolioBackend.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PortfolioBackend.Services
{
    public class TokenService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TokenService> _logger;

        public TokenService(IConfiguration configuration, ILogger<TokenService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public string CreateToken(User user)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? "your-very-secure-secret-key-that-is-at-least-256-bits-long";
            var jwtIssuer = _configuration["Jwt:Issuer"] ?? "CodenexSolutions";
            var jwtAudience = _configuration["Jwt:Audience"] ?? "CodenexSolutions";
            
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
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

            var expiresAt = DateTime.UtcNow.AddHours(24); // Token expires in 24 hours

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
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
                var jwtKey = _configuration["Jwt:Key"] ?? "your-very-secure-secret-key-that-is-at-least-256-bits-long";
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["Jwt:Issuer"] ?? "CodenexSolutions",
                    ValidateAudience = true,
                    ValidAudience = _configuration["Jwt:Audience"] ?? "CodenexSolutions",
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
    }
}
