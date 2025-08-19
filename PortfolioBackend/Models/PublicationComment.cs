using System.ComponentModel.DataAnnotations;

namespace PortfolioBackend.Models
{
    public class PublicationComment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PublicationId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public required string Comment { get; set; }

        public bool IsApproved { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Publication? Publication { get; set; }
        public virtual User? User { get; set; }
        public virtual ICollection<CommentLike> Likes { get; set; } = new List<CommentLike>();
    }
}
