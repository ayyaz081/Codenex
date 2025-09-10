using System.ComponentModel.DataAnnotations;

namespace CodeNex.DTOs
{
    public class RepositoryDto
    {
        [StringLength(200)]
        public string? Title { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(200)]
        public string? Tags { get; set; }

        [StringLength(255)]
        public string? GitHubUrl { get; set; }

        public bool? IsPremium { get; set; }

        public bool? IsFree { get; set; }

        [StringLength(100)]
        public string? License { get; set; }

        [StringLength(50)]
        public string? Version { get; set; }

        [StringLength(100)]
        public string? Category { get; set; }

        [StringLength(200)]
        public string? TechnicalStack { get; set; }

        public bool? IsActive { get; set; }
    }

    public class RepositoryCreateDto
    {
        [Required]
        [StringLength(200)]
        public required string Title { get; set; }

        [Required]
        [StringLength(1000)]
        public required string Description { get; set; }

        [StringLength(200)]
        public string? Tags { get; set; }

        [StringLength(255)]
        public string? GitHubUrl { get; set; }

        public bool IsPremium { get; set; } = false;

        public bool IsFree { get; set; } = true;

        [StringLength(100)]
        public string? License { get; set; }

        [StringLength(50)]
        public string? Version { get; set; }

        [StringLength(100)]
        public string? Category { get; set; }

        [StringLength(200)]
        public string? TechnicalStack { get; set; }
    }
}
