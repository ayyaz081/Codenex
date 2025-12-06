using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Codenex.Models
{
    [Index(nameof(DisplayOrder))]
    [Index(nameof(IsActive))]
    public class TeamMember
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string Position { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Department { get; set; }

        [MaxLength(255)]
        [EmailAddress]
        public string? Email { get; set; }

        [MaxLength(1000)]
        public string? Bio { get; set; }

        [MaxLength(255)]
        public string? PhotoUrl { get; set; }

        [MaxLength(255)]
        public string? LinkedInUrl { get; set; }

        [MaxLength(255)]
        public string? TwitterUrl { get; set; }

        public int DisplayOrder { get; set; } = 999;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
