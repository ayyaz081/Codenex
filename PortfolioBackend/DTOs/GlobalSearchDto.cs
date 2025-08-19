using System.ComponentModel.DataAnnotations;

namespace PortfolioBackend.DTOs
{
    public class GlobalSearchDto
    {
        [Required]
        [StringLength(200)]
        public string Query { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Category { get; set; } // "products", "solutions", "publications", "repositories", "all"

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class GlobalSearchResultDto
    {
        public string Type { get; set; } = string.Empty; // "product", "solution", "publication", "repository"
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? Domain { get; set; }
        public string? Category { get; set; }
        public double Relevance { get; set; } // Search relevance score
        public DateTime CreatedAt { get; set; }
    }

    public class GlobalSearchResponseDto
    {
        public string Query { get; set; } = string.Empty;
        public int TotalResults { get; set; }
        public List<GlobalSearchResultDto> Results { get; set; } = new List<GlobalSearchResultDto>();
        public Dictionary<string, int> ResultsByType { get; set; } = new Dictionary<string, int>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}
