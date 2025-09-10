using System.ComponentModel.DataAnnotations;

namespace CodeNex.Models
{
    public class Solution
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public required string Title { get; set; }

        [Required]
        [StringLength(2000)]
        public required string Summary { get; set; }

        [Required]
        [StringLength(50)]
        public required string ProblemArea { get; set; }

        [StringLength(255)]
        public string? DemoImageUrl { get; set; }

        [StringLength(255)]
        public string? DemoVideoUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
