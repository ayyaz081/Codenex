using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CodeNex.Data;

namespace CodeNex.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NavigationController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<NavigationController> _logger;

        public NavigationController(AppDbContext context, ILogger<NavigationController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/navigation/products/domains
        [HttpGet("products/domains")]
        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
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
                _logger.LogError(ex, "Error getting product domains");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/navigation/publications/domains
        [HttpGet("publications/domains")]
        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
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
                _logger.LogError(ex, "Error getting publication domains");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/navigation/repositories/categories
        [HttpGet("repositories/categories")]
        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
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
                _logger.LogError(ex, "Error getting repository categories");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/navigation/solutions/problemareas
        [HttpGet("solutions/problemareas")]
        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
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
                _logger.LogError(ex, "Error getting solution problem areas");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/navigation/all - Get all navigation data in one call
        [HttpGet("all")]
        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
        public async Task<ActionResult> GetAllNavigationData()
        {
            try
            {
                var productDomains = await _context.Products
                    .Where(p => !string.IsNullOrEmpty(p.Domain))
                    .Select(p => p.Domain)
                    .Distinct()
                    .OrderBy(d => d)
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

                var solutionProblemAreas = await _context.Solutions
                    .Where(s => s.IsActive && !string.IsNullOrEmpty(s.ProblemArea))
                    .Select(s => s.ProblemArea)
                    .Distinct()
                    .OrderBy(p => p)
                    .ToListAsync();

                return Ok(new
                {
                    products = new { domains = productDomains },
                    publications = new { domains = publicationDomains },
                    repositories = new { categories = repositoryCategories },
                    solutions = new { problemAreas = solutionProblemAreas }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all navigation data");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
