using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CodeNex.Data;
using CodeNex.Models;
using CodeNex.DTOs;
using System.Security.Claims;
using System.Text;

namespace CodeNex.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RepositoryController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<RepositoryController> _logger;

        public RepositoryController(
            AppDbContext context,
            ILogger<RepositoryController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/repository
        [HttpGet]
        [ResponseCache(Duration = 60)]
        public async Task<ActionResult<IEnumerable<Repository>>> GetRepositories()
        {
            try
            {
                return await _context.Repositories
                    .Where(r => r.IsActive)
                    .OrderByDescending(r => r.CreatedAt)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving repositories");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/repository/5
        [HttpGet("{id:int}")]
        [ResponseCache(Duration = 120)]
        public async Task<ActionResult<Repository>> GetRepository([FromRoute] int id)
        {
            try
            {
                var repository = await _context.Repositories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.Id == id && r.IsActive);

                if (repository == null)
                    return NotFound();

                // Increment download count
                var trackingRepo = await _context.Repositories.FindAsync(id);
                if (trackingRepo != null)
                {
                    trackingRepo.DownloadCount++;
                    await _context.SaveChangesAsync();
                }

                return Ok(repository);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving repository with ID {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/repository/category/{category}
        [HttpGet("category/{category}")]
        [ResponseCache(Duration = 60, VaryByQueryKeys = new[] { "category" })]
        public async Task<ActionResult<IEnumerable<Repository>>> GetByCategory([FromRoute] string category)
        {
            try
            {
                return await _context.Repositories
                    .Where(r => r.Category == category && r.IsActive)
                    .OrderByDescending(r => r.CreatedAt)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving repositories for category {category}");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/repository/free
        [HttpGet("free")]
        [ResponseCache(Duration = 60)]
        public async Task<ActionResult<IEnumerable<Repository>>> GetFreeRepositories()
        {
            try
            {
                return await _context.Repositories
                    .Where(r => r.IsFree && r.IsActive)
                    .OrderByDescending(r => r.DownloadCount)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving free repositories");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/repository/premium
        [HttpGet("premium")]
        [ResponseCache(Duration = 60)]
        public async Task<ActionResult<IEnumerable<Repository>>> GetPremiumRepositories()
        {
            try
            {
                return await _context.Repositories
                    .Where(r => r.IsPremium && r.IsActive)
                    .OrderByDescending(r => r.CreatedAt)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving premium repositories");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/repository/search
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Repository>>> SearchRepositories(
            [FromQuery] string? title,
            [FromQuery] string? tags,
            [FromQuery] string? stack)
        {
            try
            {
                var query = _context.Repositories
                    .Where(r => r.IsActive)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(title))
                    query = query.Where(r => r.Title.Contains(title) || r.Description.Contains(title));

                if (!string.IsNullOrEmpty(tags))
                    query = query.Where(r => r.Tags != null && r.Tags.Contains(tags));

                if (!string.IsNullOrEmpty(stack))
                    query = query.Where(r => r.TechnicalStack != null && r.TechnicalStack.Contains(stack));

                return await query
                    .OrderByDescending(r => r.DownloadCount)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching repositories");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/repository
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<Repository>> CreateRepository([FromBody] RepositoryCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var repository = new Repository
                {
                    Title = dto.Title,
                    Description = dto.Description,
                    Tags = dto.Tags ?? string.Empty,
                    GitHubUrl = dto.GitHubUrl ?? string.Empty,
                    IsPremium = dto.IsPremium,
                    IsFree = dto.IsFree,
                    License = dto.License ?? string.Empty,
                    Version = dto.Version ?? string.Empty,
                    Category = dto.Category ?? string.Empty,
                    TechnicalStack = dto.TechnicalStack ?? string.Empty,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true,
                    DownloadCount = 0
                };

                _context.Repositories.Add(repository);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetRepository), new { id = repository.Id }, repository);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating repository");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/repository/5
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UpdateRepository(
            [FromRoute] int id,
            [FromBody] RepositoryDto dto)
        {
            try
            {
                var existing = await _context.Repositories.FindAsync(id);
                if (existing == null || !existing.IsActive)
                    return NotFound();

                // Update only provided fields
                if (!string.IsNullOrEmpty(dto.Title))
                    existing.Title = dto.Title;
                if (!string.IsNullOrEmpty(dto.Description))
                    existing.Description = dto.Description;
                if (dto.Tags != null)
                    existing.Tags = dto.Tags;
                if (dto.GitHubUrl != null)
                    existing.GitHubUrl = dto.GitHubUrl;
                if (dto.IsPremium.HasValue)
                    existing.IsPremium = dto.IsPremium.Value;
                if (dto.IsFree.HasValue)
                    existing.IsFree = dto.IsFree.Value;
                if (dto.License != null)
                    existing.License = dto.License;
                if (dto.Version != null)
                    existing.Version = dto.Version;
                if (dto.Category != null)
                    existing.Category = dto.Category;
                if (dto.TechnicalStack != null)
                    existing.TechnicalStack = dto.TechnicalStack;
                if (dto.IsActive.HasValue)
                    existing.IsActive = dto.IsActive.Value;

                existing.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating repository {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/repository/5/download
        [HttpGet("{id:int}/download")]
        [Authorize]
        public async Task<IActionResult> DownloadRepository([FromRoute] int id)
        {
            try
            {
                _logger.LogInformation($"Download request received for repository ID: {id}");
                
                var repository = await _context.Repositories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.Id == id && r.IsActive);

                if (repository == null)
                {
                    _logger.LogWarning($"Repository not found: {id}");
                    return NotFound("Repository not found");
                }

                _logger.LogInformation($"Repository found: {repository.Title}, IsPremium: {repository.IsPremium}");

                // Check if repository is premium and requires authentication
                if (repository.IsPremium)
                {
                    // Check if user is authenticated
                    if (!User.Identity?.IsAuthenticated ?? true)
                    {
                        _logger.LogWarning($"Unauthorized access attempt for premium repository {id}");
                        return Unauthorized("Authentication required for premium repositories");
                    }

                    // Check if user is admin or has premium access
                    var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    
                    if (userRole != "Admin")
                    {
                        // For non-admin users, you could implement additional premium access checks here
                        // For now, we'll allow registered users to download premium content
                        // In a real application, you might check a subscription or payment status
                        
                        // Check if user has premium access (this would typically check a subscription table)
                        var user = await _context.Users
                            .AsNoTracking()
                            .FirstOrDefaultAsync(u => u.Id == (userId ?? ""));
                            
                        if (user == null || user.IsBlocked)
                        {
                            _logger.LogWarning($"Access denied for user {userId} to premium repository {id}");
                            return Forbid("Access denied");
                        }
                        
                        // In a real scenario, check if user has active premium subscription
                        // For demo purposes, we'll allow registered users to access premium content
                    }
                }

                // Increment download count
                var trackingRepo = await _context.Repositories.FindAsync(id);
                if (trackingRepo != null)
                {
                    trackingRepo.DownloadCount++;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Download count updated for repository {id}: {trackingRepo.DownloadCount}");
                }

                // In a real application, this would stream the actual file content
                // For demo purposes, we'll return a placeholder ZIP content
                var fileName = $"{repository.Title.Replace(" ", "_").Replace("/", "_")}_v{repository.Version}.zip";
                var fileContent = GeneratePlaceholderZip(repository);
                
                _logger.LogInformation($"Serving download for repository {id}, filename: {fileName}, size: {fileContent.Length} bytes");
                
                // Add explicit CORS headers for download
                Response.Headers["Access-Control-Allow-Origin"] = "*";
                Response.Headers["Access-Control-Allow-Methods"] = "GET";
                Response.Headers["Access-Control-Allow-Headers"] = "*";
                Response.Headers["Access-Control-Expose-Headers"] = "Content-Disposition, Content-Length, Content-Type";
                
                return File(fileContent, "application/zip", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error downloading repository {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/repository/categories
        [HttpGet("categories")]
        [ResponseCache(Duration = 3600)] // Cache for 1 hour
        public async Task<ActionResult<IEnumerable<string>>> GetCategories()
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

        // GET: api/repository/categories/detailed
        [HttpGet("categories/detailed")]
        [ResponseCache(Duration = 3600)] // Cache for 1 hour
        public async Task<ActionResult<IEnumerable<object>>> GetCategoriesDetailed()
        {
            try
            {
                var categories = await _context.Repositories
                    .Where(r => r.IsActive && !string.IsNullOrEmpty(r.Category))
                    .GroupBy(r => r.Category)
                    .Select(g => new
                    {
                        category = g.Key,
                        count = g.Count(),
                        repositories = g.Take(3).Select(r => new
                        {
                            id = r.Id,
                            title = r.Title,
                            description = r.Description.Length > 100 ? r.Description.Substring(0, 100) + "..." : r.Description,
                            technicalStack = r.TechnicalStack
                        })
                    })
                    .OrderByDescending(c => c.count)
                    .ToListAsync();

                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving detailed repository categories");
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: api/repository/5
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeleteRepository([FromRoute] int id)
        {
            try
            {
                var repository = await _context.Repositories.FindAsync(id);
                if (repository == null)
                    return NotFound();

                // Soft delete
                repository.IsActive = false;
                repository.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting repository {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        private byte[] GeneratePlaceholderZip(Repository repository)
        {
            // This is a placeholder implementation
            // In a real application, you would:
            // 1. Clone from GitHub URL or fetch from file storage
            // 2. Create a proper ZIP archive
            // 3. Return the actual file content
            
            var readmeContent = $@"# {repository.Title}

## Description
{repository.Description}

## Version
{repository.Version}

## License
{repository.License}

## Technical Stack
{repository.TechnicalStack}

## GitHub Repository
{repository.GitHubUrl}

## Installation
1. Extract this ZIP file
2. Follow the instructions in the project documentation
3. Refer to the GitHub repository for the latest updates

---
This is a placeholder download. In production, this would contain the actual repository files.
";
            
            return Encoding.UTF8.GetBytes(readmeContent);
        }
    }
}
