using System.ComponentModel.DataAnnotations;

namespace Neelsol.Models
{
    public class CommentLike
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CommentId { get; set; }

        public string? UserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual PublicationComment? Comment { get; set; }
        public virtual User? User { get; set; }
    }
}
