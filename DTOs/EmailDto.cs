using System.ComponentModel.DataAnnotations;

namespace Codenex.DTOs
{
    public class EmailDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
