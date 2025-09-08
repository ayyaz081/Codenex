using System.ComponentModel.DataAnnotations;

namespace PortfolioBackend.DTOs
{
    public class EmailDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
