using System.ComponentModel.DataAnnotations;

namespace PortfolioBackend.DTOs
{
    public class ResendVerificationDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
