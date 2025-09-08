using System.ComponentModel.DataAnnotations;

namespace PortfolioBackend.DTOs
{
    public class TestEmailDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
