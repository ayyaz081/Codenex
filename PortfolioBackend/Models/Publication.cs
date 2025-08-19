using System.ComponentModel.DataAnnotations;

namespace PortfolioBackend.Models
{
    public class Publication
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public required string Title { get; set; }

        [Required]
        [StringLength(500)]
        public required string Authors { get; set; }

        [Required]
        [StringLength(100)]
        public required string Domain { get; set; } // AI, ML, Cloud, etc.

        [Required]
        [StringLength(2000)]
        public required string Abstract { get; set; }

        [StringLength(255)]
        public required string ThumbnailUrl { get; set; } = string.Empty;

        [StringLength(255)]
        public required string DownloadUrl { get; set; } = string.Empty;

        [StringLength(500)]
        public required string Keywords { get; set; } = string.Empty;

        public bool IsPublished { get; set; } = true;

        public double AverageRating { get; set; } = 0.0;

        public int RatingCount { get; set; } = 0;

        public DateTime PublishedDate { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<PublicationComment> Comments { get; set; } = new List<PublicationComment>();
        public virtual ICollection<PublicationRating> Ratings { get; set; } = new List<PublicationRating>();
    }
}
