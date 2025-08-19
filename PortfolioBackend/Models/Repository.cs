using System.ComponentModel.DataAnnotations;

namespace PortfolioBackend.Models
{
    public class Repository
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public required string Title { get; set; }

        [Required]
        [StringLength(1000)]
        public required string Description { get; set; }

        [StringLength(200)]
        public required string Tags { get; set; } = string.Empty; // Comma-separated tags

        [StringLength(255)]
        public required string GitHubUrl { get; set; } = string.Empty;

        public bool IsPremium { get; set; } = false;

        public bool IsFree { get; set; } = true;

        [StringLength(100)]
        public required string License { get; set; } = string.Empty;

        [StringLength(50)]
        public required string Version { get; set; } = string.Empty;

        [StringLength(100)]
        public required string Category { get; set; } = string.Empty; // Web, Mobile, Desktop, API, etc.

        [StringLength(200)]
        public required string TechnicalStack { get; set; } = string.Empty;

        public int DownloadCount { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
