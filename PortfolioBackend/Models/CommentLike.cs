using System.ComponentModel.DataAnnotations;

namespace PortfolioBackend.Models
{
    public class CommentLike
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CommentId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual PublicationComment? Comment { get; set; }
        public virtual User? User { get; set; }
    }
}
