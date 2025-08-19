using System.ComponentModel.DataAnnotations;

namespace PortfolioBackend.Models
{
    public class ContactForm
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public required string Name { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public required string Email { get; set; }

        [Required]
        [StringLength(200)]
        public required string Subject { get; set; }

        [Required]
        [StringLength(2000)]
        public required string Message { get; set; }

        public string? UserId { get; set; } // Optional - for registered users

        public bool IsRead { get; set; } = false;

        public bool IsReplied { get; set; } = false;

        [StringLength(2000)]
        public string? AdminReply { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User? User { get; set; }
    }
}
