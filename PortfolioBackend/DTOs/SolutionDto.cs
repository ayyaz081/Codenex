using System.ComponentModel.DataAnnotations;

namespace PortfolioBackend.Models
{
    public class SolutionDto
    {
        [StringLength(100)]
        public string? Title { get; set; } // Optional for updates

        [StringLength(2000)]
        public string? Summary { get; set; } // Optional for updates

        [StringLength(50)]
        public string? ProblemArea { get; set; } // Optional for updates

        [StringLength(255)]
        public string? DemoVideoUrl { get; set; }

        [StringLength(255)]
        public string? DemoImageUrl { get; set; }
    }
}