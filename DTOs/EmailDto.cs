using System.ComponentModel.DataAnnotations;

namespace Neelsol.DTOs
{
    public class EmailDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
