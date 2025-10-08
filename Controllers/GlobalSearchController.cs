using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CodeNex.Data;
using CodeNex.DTOs;

namespace CodeNex.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GlobalSearchController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<GlobalSearchController> _logger;
        private static readonly Random _random = new();

        public GlobalSearchController(
            AppDbContext context,
            ILogger<GlobalSearchController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/globalsearch
        [HttpGet]
        [ResponseCache(Duration = 30, VaryByQueryKeys = new[] { "query", "category", "page", "pageSize" })]
        public async Task<ActionResult<GlobalSearchResponseDto>> GlobalSearch(
            [FromQuery] string query,
            [FromQuery] string? category = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Search query is required.");

            if (pageSize > 50)
                pageSize = 50; // Limit page size

            try
            {
                var results = new List<GlobalSearchResultDto>();
                var resultsByType = new Dictionary<string, int>();

                // Search categories sequentially to avoid potential issues
                var searchCategories = GetSearchCategories(category);

                foreach (var searchCategory in searchCategories)
                {
                    var categoryResults = await SearchByCategory(query, searchCategory);
                    results.AddRange(categoryResults);
                    resultsByType[searchCategory] = categoryResults.Count;
                }

                // Sort by relevance and apply pagination
                var sortedResults = results
                    .OrderByDescending(r => r.Relevance)
                    .ThenByDescending(r => r.CreatedAt)
                    .ToList();

                var totalResults = sortedResults.Count;
                var paginatedResults = sortedResults
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var response = new GlobalSearchResponseDto
                {
                    Query = query,
                    TotalResults = totalResults,
                    Results = paginatedResults,
                    ResultsByType = resultsByType,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalResults / pageSize)
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing global search for query: {Query}", query);
                return StatusCode(500, new { error = ex.Message, query = query });
            }
        }

        // GET: api/globalsearch/test - Simple test endpoint
        [HttpGet("test")]
        public async Task<ActionResult> TestSearch([FromQuery] string query = "test")
        {
            try
            {
                var productCount = await _context.Products.CountAsync();
                var solutionCount = await _context.Solutions.CountAsync();
                var publicationCount = await _context.Publications.CountAsync();
                var repositoryCount = await _context.Repositories.CountAsync();
                
                return Ok(new {
                    message = "GlobalSearch controller is working",
                    query,
                    counts = new {
                        products = productCount,
                        solutions = solutionCount,
                        publications = publicationCount,
                        repositories = repositoryCount
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Test endpoint error");
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        // GET: api/globalsearch/minimal - Minimal search test
        [HttpGet("minimal")]
        public async Task<ActionResult> MinimalSearch([FromQuery] string query = "test")
        {
            try
            {
                _logger.LogInformation("Starting minimal search for query: {Query}", query);
                
                // Test just products first
                var products = await _context.Products
                    .Where(p => p.Title.Contains(query))
                    .Take(2)
                    .Select(p => new
                    {
                        p.Id,
                        p.Title,
                        p.ShortDescription
                    })
                    .ToListAsync();
                
                _logger.LogInformation("Found {Count} products", products.Count);
                
                return Ok(new {
                    query,
                    productCount = products.Count,
                    products = products.Take(1).ToList() // Just return first product
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Minimal search error for query: {Query}", query);
                return StatusCode(500, new { error = ex.Message, innerException = ex.InnerException?.Message });
            }
        }

        // GET: api/globalsearch/suggestions
        [HttpGet("suggestions")]
        [ResponseCache(Duration = 60, VaryByQueryKeys = new[] { "query" })]
        public async Task<ActionResult<List<string>>> GetSearchSuggestions([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return Ok(new List<string>());

            try
            {
                var suggestions = new HashSet<string>();

                // Get suggestions from different entities in parallel for better performance
                var suggestionTasks = new[]
                {
                    _context.Products.Where(p => p.Title.Contains(query)).Select(p => p.Title).Take(5).ToListAsync(),
                    _context.Solutions.Where(s => s.IsActive && s.Title.Contains(query)).Select(s => s.Title).Take(5).ToListAsync(),
                    _context.Publications.Where(p => p.IsPublished && p.Title.Contains(query)).Select(p => p.Title).Take(5).ToListAsync(),
                    _context.Repositories.Where(r => r.IsActive && r.Title.Contains(query)).Select(r => r.Title).Take(5).ToListAsync(),
                    _context.Products.Where(p => p.Domain.Contains(query)).Select(p => p.Domain).Distinct().Take(3).ToListAsync()
                };

                var suggestionResults = await Task.WhenAll(suggestionTasks);
                var productTitles = suggestionResults[0];
                var solutionTitles = suggestionResults[1];
                var publicationTitles = suggestionResults[2];
                var repositoryTitles = suggestionResults[3];
                var domains = suggestionResults[4];

                suggestions.UnionWith(productTitles);
                suggestions.UnionWith(solutionTitles);
                suggestions.UnionWith(publicationTitles);
                suggestions.UnionWith(repositoryTitles);
                suggestions.UnionWith(domains);

                return Ok(suggestions.Take(10).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting search suggestions for query: {Query}", query);
                return StatusCode(500, "Internal server error");
            }
        }

        #region Private Helper Methods

        private static List<string> GetSearchCategories(string? category)
        {
            return category?.ToLower() switch
            {
                "products" => new List<string> { "products" },
                "solutions" => new List<string> { "solutions" },
                "publications" => new List<string> { "publications" },
                "repositories" => new List<string> { "repositories" },
                _ => new List<string> { "products", "solutions", "publications", "repositories" }
            };
        }

        private async Task<List<GlobalSearchResultDto>> SearchByCategory(string query, string category)
        {
            return category.ToLower() switch
            {
                "products" => await SearchProducts(query),
                "solutions" => await SearchSolutions(query),
                "publications" => await SearchPublications(query),
                "repositories" => await SearchRepositories(query),
                _ => new List<GlobalSearchResultDto>()
            };
        }

        private async Task<List<GlobalSearchResultDto>> SearchProducts(string query)
        {
            var products = await _context.Products
                .Where(p => p.Title.Contains(query) || 
                           p.ShortDescription.Contains(query) || 
                           p.LongDescription.Contains(query) ||
                           p.Domain.Contains(query))
                .Take(50) // Limit results for performance
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.ShortDescription,
                    p.ImageUrl,
                    p.Domain,
                    p.CreatedAt
                })
                .ToListAsync();

            return products.Select(p => new GlobalSearchResultDto
            {
                Type = "product",
                Id = p.Id,
                Title = p.Title,
                Description = p.ShortDescription,
                Url = $"Products.html?id={p.Id}",
                ImageUrl = p.ImageUrl,
                Domain = p.Domain,
                Category = p.Domain,
                Relevance = CalculateRelevance(query, p.Title, p.ShortDescription ?? ""),
                CreatedAt = p.CreatedAt
            }).ToList();
        }

        private async Task<List<GlobalSearchResultDto>> SearchSolutions(string query)
        {
            var solutions = await _context.Solutions
                .Where(s => s.IsActive && (s.Title.Contains(query) || 
                                          s.Summary.Contains(query) ||
                                          s.ProblemArea.Contains(query)))
                .Take(50) // Limit results for performance
                .Select(s => new
                {
                    s.Id,
                    s.Title,
                    s.Summary,
                    s.DemoImageUrl,
                    s.ProblemArea,
                    s.CreatedAt
                })
                .ToListAsync();

            return solutions.Select(s => new GlobalSearchResultDto
            {
                Type = "solution",
                Id = s.Id,
                Title = s.Title,
                Description = s.Summary,
                Url = $"Solutions.html?id={s.Id}",
                ImageUrl = s.DemoImageUrl,
                Domain = null,
                Category = s.ProblemArea,
                Relevance = CalculateRelevance(query, s.Title, s.Summary ?? ""),
                CreatedAt = s.CreatedAt
            }).ToList();
        }

        private async Task<List<GlobalSearchResultDto>> SearchPublications(string query)
        {
            var publications = await _context.Publications
                .Where(p => p.IsPublished && (p.Title.Contains(query) || 
                                             p.Authors.Contains(query) ||
                                             p.Abstract.Contains(query) ||
                                             p.Keywords.Contains(query) ||
                                             p.Domain.Contains(query)))
                .Take(50) // Limit results for performance
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.Abstract,
                    p.ThumbnailUrl,
                    p.Domain,
                    p.CreatedAt
                })
                .ToListAsync();

            return publications.Select(p => new GlobalSearchResultDto
            {
                Type = "publication",
                Id = p.Id,
                Title = p.Title,
                Description = p.Abstract,
                Url = $"Publications.html?id={p.Id}",
                ImageUrl = p.ThumbnailUrl,
                Domain = p.Domain,
                Category = p.Domain,
                Relevance = CalculateRelevance(query, p.Title, p.Abstract ?? ""),
                CreatedAt = p.CreatedAt
            }).ToList();
        }

        private async Task<List<GlobalSearchResultDto>> SearchRepositories(string query)
        {
            var repositories = await _context.Repositories
                .Where(r => r.IsActive && (r.Title.Contains(query) || 
                                          r.Description.Contains(query) ||
                                          r.TechnicalStack.Contains(query) ||
                                          r.Category.Contains(query)))
                .Take(50) // Limit results for performance
                .Select(r => new
                {
                    r.Id,
                    r.Title,
                    r.Description,
                    r.Category,
                    r.CreatedAt
                })
                .ToListAsync();

            return repositories.Select(r => new GlobalSearchResultDto
            {
                Type = "repository",
                Id = r.Id,
                Title = r.Title,
                Description = r.Description,
                Url = $"Repository.html?id={r.Id}",
                ImageUrl = null,
                Domain = null,
                Category = r.Category,
                Relevance = CalculateRelevance(query, r.Title, r.Description ?? ""),
                CreatedAt = r.CreatedAt
            }).ToList();
        }

        private static double CalculateRelevance(string query, string title, string description)
        {
            double score = 0.0;
            var queryLower = query.ToLower();
            var titleLower = title.ToLower();
            var descriptionLower = description.ToLower();

            // Exact title match gets highest score
            if (titleLower == queryLower)
                score += 100.0;
            else if (titleLower.Contains(queryLower))
                score += 50.0;

            // Title starts with query gets high score
            if (titleLower.StartsWith(queryLower))
                score += 30.0;

            // Description contains query
            if (descriptionLower.Contains(queryLower))
                score += 20.0;

            // Boost score for shorter titles (more specific matches)
            if (title.Length < 50)
                score += 10.0;

            // Add some randomness to avoid always same order for equal scores
            score += _random.NextDouble() * 5;

            return score;
        }

        #endregion
    }
}
