using System.ComponentModel.DataAnnotations;

namespace CodeNex.Models
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

        [StringLength(255)]
        public required string GitHubUrl { get; set; } = string.Empty;

        public bool IsPremium { get; set; } = false;

        public bool IsFree { get; set; } = true;

        [StringLength(100)]
        public required string Category { get; set; } = string.Empty; // Web, Mobile, Desktop, API, etc.

    [StringLength(200)]
    public required string TechnicalStack { get; set; } = string.Empty;

    public int DownloadCount { get; set; } = 0;

    // Price for premium repositories (null for free)
    public decimal? Price { get; set; }

    // GitHub organization repo full name (e.g., "CodeNex-Premium/repo-name")
    [StringLength(255)]
    public string? GitHubRepoFullName { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Foreign key
    [Required]
    public int ProductId { get; set; }

    // Navigation property
    public virtual Product Product { get; set; } = null!;
    }
}
