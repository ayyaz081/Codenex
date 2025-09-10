using System.ComponentModel.DataAnnotations;

namespace CodeNex.DTOs
{
    public class EmailDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
