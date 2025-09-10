using Microsoft.IdentityModel.Tokens;
using CodeNex.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CodeNex.Services
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
            var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? 
                         _configuration["Jwt:Key"] ?? 
                         "your-secure-jwt-key-at-least-256-bits-long-replace-this-in-production";
            var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? 
                            _configuration["Jwt:Issuer"] ?? "CodeNexAPI";
            var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? 
                              _configuration["Jwt:Audience"] ?? "CodeNexAPI";
            
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
                var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? 
                             _configuration["Jwt:Key"] ?? 
                             "your-secure-jwt-key-at-least-256-bits-long-replace-this-in-production";
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? 
                                  _configuration["Jwt:Issuer"] ?? "CodeNexAPI",
                    ValidateAudience = true,
                    ValidAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? 
                                    _configuration["Jwt:Audience"] ?? "CodeNexAPI",
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
