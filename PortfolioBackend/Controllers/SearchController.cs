using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortfolioBackend.Data;
using PortfolioBackend.Models;
using System.Text.Json;

namespace PortfolioBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SearchController> _logger;

        public SearchController(
            AppDbContext context,
            ILogger<SearchController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("global")]
        public async Task<ActionResult<IEnumerable<GlobalSearchResult>>> GlobalSearch([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("Query parameter is required.");
            }

            try
            {
                var searchResults = new List<GlobalSearchResult>();

                // Search Publications
                try
                {
                    var publications = await _context.Publications.ToListAsync();
                    var publicationResults = publications
                        .Where(p => ContainsQuery(p.Title, query) ||
                                   ContainsQuery(p.Abstract, query) ||
                                   ContainsQuery(p.Authors, query) ||
                                   ContainsQuery(p.Keywords, query))
                        .Take(3)
                        .Select(p => new GlobalSearchResult
                        {
                            Type = "publication",
                            Title = p.Title,
                            Description = TruncateString(p.Abstract, 100),
                            Url = $"Publications.html?id={p.Id}",
                            Meta = $"{p.Authors} • {p.PublishedDate.Year}"
                        });
                    searchResults.AddRange(publicationResults);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error searching publications");
                }

                // Search Products
                try
                {
                    var products = await _context.Products.ToListAsync();
                    var productResults = products
                        .Where(p => ContainsQuery(p.Title, query) ||
                                   ContainsQuery(p.ShortDescription, query) ||
                                   ContainsQuery(p.LongDescription, query) ||
                                   ContainsQuery(p.Domain, query))
                        .Take(3)
                        .Select(p => new GlobalSearchResult
                        {
                            Type = "product",
                            Title = p.Title,
                            Description = TruncateString(p.ShortDescription, 100),
                            Url = $"Products.html?id={p.Id}",
                            Meta = $"{p.Domain}"
                        });
                    searchResults.AddRange(productResults);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error searching products");
                }

                // Search Solutions
                try
                {
                    var solutions = await _context.Solutions.ToListAsync();
                    var solutionResults = solutions
                        .Where(s => ContainsQuery(s.Title, query) ||
                                   ContainsQuery(s.Summary, query) ||
                                   ContainsQuery(s.ProblemArea, query))
                        .Take(3)
                        .Select(s => new GlobalSearchResult
                        {
                            Type = "solution",
                            Title = s.Title,
                            Description = TruncateString(s.Summary, 100),
                            Url = $"Solutions.html?id={s.Id}",
                            Meta = $"{s.ProblemArea} • {(s.IsActive ? "Active" : "Inactive")}"
                        });
                    searchResults.AddRange(solutionResults);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error searching solutions");
                }

                // Search Repositories
                try
                {
                    var repositories = await _context.Repositories.ToListAsync();
                    var repositoryResults = repositories
                        .Where(r => ContainsQuery(r.Title, query) ||
                                   ContainsQuery(r.Description, query) ||
                                   ContainsQuery(r.Category, query) ||
                                   ContainsQuery(r.TechnicalStack, query))
                        .Take(3)
                        .Select(r => new GlobalSearchResult
                        {
                            Type = "repository",
                            Title = r.Title,
                            Description = TruncateString(r.Description, 100),
                            Url = $"Repository.html?id={r.Id}",
                            Meta = $"{r.Category} • {(r.IsFree ? "Free" : "Premium")}"
                        });
                    searchResults.AddRange(repositoryResults);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error searching repositories");
                }

                // Add static pages if they match
                var staticPages = GetStaticPages()
                    .Where(page => ContainsQuery(page.Title, query) ||
                                  ContainsQuery(page.Description, query))
                    .Take(2);
                searchResults.AddRange(staticPages);

                // Order by relevance (exact matches first, then partial matches)
                var orderedResults = searchResults
                    .OrderBy(r => GetRelevanceScore(r, query))
                    .Take(8)
                    .ToList();

                return Ok(orderedResults);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing global search for query: {Query}", query);
                return StatusCode(500, "An error occurred while searching.");
            }
        }

        [HttpGet("suggestions")]
        public async Task<ActionResult<IEnumerable<string>>> GetSuggestions([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                return Ok(Array.Empty<string>());
            }

            try
            {
                var suggestions = new HashSet<string>();

                // Get suggestions from different sources
                var publications = await _context.Publications.ToListAsync();
                var products = await _context.Products.ToListAsync();
                var solutions = await _context.Solutions.ToListAsync();
                var repositories = await _context.Repositories.ToListAsync();

                // Add publication-related suggestions
                foreach (var pub in publications.Take(20))
                {
                    AddSuggestionIfMatches(suggestions, pub.Title, query);
                    AddSuggestionIfMatches(suggestions, pub.Authors, query);
                    if (!string.IsNullOrEmpty(pub.Keywords))
                    {
                        foreach (var keyword in pub.Keywords.Split(','))
                        {
                            AddSuggestionIfMatches(suggestions, keyword.Trim(), query);
                        }
                    }
                }

                // Add product-related suggestions
                foreach (var product in products.Take(20))
                {
                    AddSuggestionIfMatches(suggestions, product.Title, query);
                    AddSuggestionIfMatches(suggestions, product.Domain, query);
                }

                // Add solution-related suggestions
                foreach (var solution in solutions.Take(20))
                {
                    AddSuggestionIfMatches(suggestions, solution.Title, query);
                    AddSuggestionIfMatches(suggestions, solution.ProblemArea, query);
                }

                // Add repository-related suggestions
                foreach (var repo in repositories.Take(20))
                {
                    AddSuggestionIfMatches(suggestions, repo.Title, query);
                    AddSuggestionIfMatches(suggestions, repo.Category, query);
                    if (!string.IsNullOrEmpty(repo.TechnicalStack))
                    {
                        foreach (var tech in repo.TechnicalStack.Split(','))
                        {
                            AddSuggestionIfMatches(suggestions, tech.Trim(), query);
                        }
                    }
                }

                return Ok(suggestions.Take(8).OrderBy(s => s).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting search suggestions for query: {Query}", query);
                return Ok(Array.Empty<string>());
            }
        }

        private bool ContainsQuery(string? text, string query)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            return text.Contains(query, StringComparison.OrdinalIgnoreCase);
        }

        private void AddSuggestionIfMatches(HashSet<string> suggestions, string? text, string query)
        {
            if (!string.IsNullOrWhiteSpace(text) && 
                text.Contains(query, StringComparison.OrdinalIgnoreCase) &&
                text.Length <= 50)
            {
                suggestions.Add(text);
            }
        }

        private string TruncateString(string? text, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";

            return text.Length <= maxLength ? text : text.Substring(0, maxLength) + "...";
        }

        private int GetRelevanceScore(GlobalSearchResult result, string query)
        {
            var score = 0;
            var lowerQuery = query.ToLower();
            var lowerTitle = result.Title.ToLower();
            var lowerDescription = result.Description.ToLower();

            // Exact title match gets highest priority
            if (lowerTitle == lowerQuery) score += 1000;
            // Title starts with query
            else if (lowerTitle.StartsWith(lowerQuery)) score += 100;
            // Title contains query
            else if (lowerTitle.Contains(lowerQuery)) score += 10;
            // Description contains query
            else if (lowerDescription.Contains(lowerQuery)) score += 1;

            return score;
        }

        private List<GlobalSearchResult> GetStaticPages()
        {
            return new List<GlobalSearchResult>
            {
                new GlobalSearchResult
                {
                    Type = "page",
                    Title = "Home",
                    Description = "Welcome to Codenex Solutions - Your technology partner",
                    Url = "Home.html"
                },
                new GlobalSearchResult
                {
                    Type = "page",
                    Title = "About",
                    Description = "Learn more about Codenex Solutions and our team",
                    Url = "About.html"
                },
                new GlobalSearchResult
                {
                    Type = "page",
                    Title = "Contact",
                    Description = "Get in touch with our team",
                    Url = "Contact.html"
                },
                new GlobalSearchResult
                {
                    Type = "page",
                    Title = "Publications",
                    Description = "Research papers and academic publications",
                    Url = "Publications.html"
                },
                new GlobalSearchResult
                {
                    Type = "page",
                    Title = "Products",
                    Description = "Our software products and solutions",
                    Url = "Products.html"
                },
                new GlobalSearchResult
                {
                    Type = "page",
                    Title = "Solutions",
                    Description = "Custom solutions for your business needs",
                    Url = "Solutions.html"
                },
                new GlobalSearchResult
                {
                    Type = "page",
                    Title = "Repository",
                    Description = "Code repositories and open source projects",
                    Url = "Repository.html"
                }
            };
        }
    }

    public class GlobalSearchResult
    {
        public string Type { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Url { get; set; } = "";
        public string Meta { get; set; } = "";
    }
}
