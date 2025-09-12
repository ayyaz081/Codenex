using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CodeNex.Data;

namespace CodeNex.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(AppDbContext context, ILogger<CategoriesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/categories/product-domains
        [HttpGet("product-domains")]
        [ResponseCache(Duration = 300)] // Cache for 5 minutes
        public async Task<ActionResult<List<string>>> GetProductDomains()
        {
            try
            {
                var domains = await _context.Products
                    .Where(p => !string.IsNullOrEmpty(p.Domain))
                    .Select(p => p.Domain)
                    .Distinct()
                    .OrderBy(d => d)
                    .ToListAsync();

                return Ok(domains);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product domains");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/categories/solution-problem-areas
        [HttpGet("solution-problem-areas")]
        [ResponseCache(Duration = 300)] // Cache for 5 minutes
        public async Task<ActionResult<List<string>>> GetSolutionProblemAreas()
        {
            try
            {
                var problemAreas = await _context.Solutions
                    .Where(s => s.IsActive && !string.IsNullOrEmpty(s.ProblemArea))
                    .Select(s => s.ProblemArea)
                    .Distinct()
                    .OrderBy(p => p)
                    .ToListAsync();

                return Ok(problemAreas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving solution problem areas");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/categories/publication-domains
        [HttpGet("publication-domains")]
        [ResponseCache(Duration = 300)] // Cache for 5 minutes
        public async Task<ActionResult<List<string>>> GetPublicationDomains()
        {
            try
            {
                var domains = await _context.Publications
                    .Where(p => p.IsPublished && !string.IsNullOrEmpty(p.Domain))
                    .Select(p => p.Domain)
                    .Distinct()
                    .OrderBy(d => d)
                    .ToListAsync();

                return Ok(domains);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving publication domains");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/categories/repository-categories
        [HttpGet("repository-categories")]
        [ResponseCache(Duration = 300)] // Cache for 5 minutes
        public async Task<ActionResult<List<string>>> GetRepositoryCategories()
        {
            try
            {
                var categories = await _context.Repositories
                    .Where(r => r.IsActive && !string.IsNullOrEmpty(r.Category))
                    .Select(r => r.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving repository categories");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/categories/all
        [HttpGet("all")]
        [ResponseCache(Duration = 300)] // Cache for 5 minutes
        public async Task<ActionResult<object>> GetAllCategories()
        {
            try
            {
                // Execute queries sequentially to avoid DbContext concurrency issues
                var productDomains = await _context.Products
                    .Where(p => !string.IsNullOrEmpty(p.Domain))
                    .Select(p => p.Domain)
                    .Distinct()
                    .OrderBy(d => d)
                    .ToListAsync();
                
                var solutionProblemAreas = await _context.Solutions
                    .Where(s => s.IsActive && !string.IsNullOrEmpty(s.ProblemArea))
                    .Select(s => s.ProblemArea)
                    .Distinct()
                    .OrderBy(p => p)
                    .ToListAsync();
                
                var publicationDomains = await _context.Publications
                    .Where(p => p.IsPublished && !string.IsNullOrEmpty(p.Domain))
                    .Select(p => p.Domain)
                    .Distinct()
                    .OrderBy(d => d)
                    .ToListAsync();
                
                var repositoryCategories = await _context.Repositories
                    .Where(r => r.IsActive && !string.IsNullOrEmpty(r.Category))
                    .Select(r => r.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                return Ok(new
                {
                    ProductDomains = productDomains,
                    SolutionProblemAreas = solutionProblemAreas,
                    PublicationDomains = publicationDomains,
                    RepositoryCategories = repositoryCategories
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all categories");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/categories/search?type={type}&query={query}
        [HttpGet("search")]
        [ResponseCache(Duration = 60)] // Cache for 1 minute
        public async Task<ActionResult<List<string>>> SearchCategories([FromQuery] string type, [FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return Ok(new List<string>());

            try
            {
                List<string> results = new List<string>();

                switch (type?.ToLower())
                {
                    case "product-domains":
                        results = await _context.Products
                            .Where(p => !string.IsNullOrEmpty(p.Domain) && p.Domain.Contains(query))
                            .Select(p => p.Domain)
                            .Distinct()
                            .OrderBy(d => d)
                            .Take(10)
                            .ToListAsync();
                        break;

                    case "solution-problem-areas":
                        results = await _context.Solutions
                            .Where(s => s.IsActive && !string.IsNullOrEmpty(s.ProblemArea) && s.ProblemArea.Contains(query))
                            .Select(s => s.ProblemArea)
                            .Distinct()
                            .OrderBy(p => p)
                            .Take(10)
                            .ToListAsync();
                        break;

                    case "publication-domains":
                        results = await _context.Publications
                            .Where(p => p.IsPublished && !string.IsNullOrEmpty(p.Domain) && p.Domain.Contains(query))
                            .Select(p => p.Domain)
                            .Distinct()
                            .OrderBy(d => d)
                            .Take(10)
                            .ToListAsync();
                        break;

                    case "repository-categories":
                        results = await _context.Repositories
                            .Where(r => r.IsActive && !string.IsNullOrEmpty(r.Category) && r.Category.Contains(query))
                            .Select(r => r.Category)
                            .Distinct()
                            .OrderBy(c => c)
                            .Take(10)
                            .ToListAsync();
                        break;

                    default:
                        return BadRequest("Invalid category type. Supported types: product-domains, solution-problem-areas, publication-domains, repository-categories");
                }

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching categories for type: {Type}, query: {Query}", type, query);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
