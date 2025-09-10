using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace CodeNex.Models
{
    public class SolutionFormDto
    {
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
        public string? DemoVideoUrl { get; set; }

        public IFormFile? DemoImageFile { get; set; }
    }
}
