using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CodeNex.Data;
using CodeNex.Models;
using CodeNex.Exceptions;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;

namespace CodeNex.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SolutionsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<SolutionsController> _logger;

        // File upload constraints
        private const long MaxFileSize = 5 * 1024 * 1024; // 5MB
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".svg" };

        public SolutionsController(
            AppDbContext context,
            IWebHostEnvironment env,
            ILogger<SolutionsController> logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
        }

        // GET: api/solutions
        [HttpGet]
        [ResponseCache(Duration = 30)]
        public async Task<ActionResult<IEnumerable<Solution>>> GetSolutions(
            [FromQuery] string? problemArea = null,
            [FromQuery] string? category = null,
            [FromQuery] string? domain = null,
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var query = _context.Solutions
                    .Where(s => s.IsActive);

                // Apply problem area filter (problemArea, category, and domain all map to ProblemArea)
                var filterValue = problemArea ?? category ?? domain;
                if (!string.IsNullOrEmpty(filterValue))
                {
                    query = query.Where(s => s.ProblemArea.ToLower() == filterValue.ToLower());
                }

                // Apply search filter across Title, Summary, and ProblemArea
                if (!string.IsNullOrEmpty(search))
                {
                    var searchLower = search.ToLower();
                    query = query.Where(s => 
                        s.Title.ToLower().Contains(searchLower) ||
                        s.Summary.ToLower().Contains(searchLower) ||
                        s.ProblemArea.ToLower().Contains(searchLower));
                }

                var solutions = await query
                    .Include(s => s.Publications)
                    .OrderByDescending(s => s.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .AsNoTracking()
                    .ToListAsync();

                return Ok(solutions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving solutions");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/solutions/5
        [HttpGet("{id:int}")]
        [ResponseCache(Duration = 60)]
        public async Task<ActionResult<Solution>> GetSolution([FromRoute] int id)
        {
            try
            {
                var solution = await _context.Solutions
                    .Include(s => s.Publications)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == id && s.IsActive);

                return solution == null ? NotFound() : Ok(solution);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving solution with ID {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/solutions/problem-areas/{problemArea}
        [HttpGet("problem-areas/{problemArea}")]
        [ResponseCache(Duration = 60, VaryByQueryKeys = new[] { "problemArea" })]
        public async Task<ActionResult<IEnumerable<Solution>>> GetByProblemArea([FromRoute] string problemArea)
        {
            try
            {
                return await _context.Solutions
                    .Where(s => s.ProblemArea == problemArea && s.IsActive)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving solutions for problem area {problemArea}");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/solutions/problem-areas
        [HttpGet("problem-areas")]
        [ResponseCache(Duration = 3600)] // Cache for 1 hour
        public async Task<ActionResult<IEnumerable<string>>> GetProblemAreas()
        {
            try
            {
                var problemAreas = await _context.Solutions
                    .Where(s => s.IsActive && !string.IsNullOrEmpty(s.ProblemArea))
                    .Select(s => s.ProblemArea)
                    .Distinct()
                    .OrderBy(pa => pa)
                    .ToListAsync();

                return Ok(problemAreas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving problem areas");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/solutions/problem-areas/detailed
        [HttpGet("problem-areas/detailed")]
        [ResponseCache(Duration = 3600)] // Cache for 1 hour
        public async Task<ActionResult<IEnumerable<object>>> GetProblemAreasDetailed()
        {
            try
            {
                var problemAreas = await _context.Solutions
                    .Where(s => s.IsActive && !string.IsNullOrEmpty(s.ProblemArea))
                    .GroupBy(s => s.ProblemArea)
                    .Select(g => new
                    {
                        problemArea = g.Key,
                        count = g.Count(),
                        solutions = g.Take(3).Select(s => new
                        {
                            id = s.Id,
                            title = s.Title,
                            summary = s.Summary.Length > 100 ? s.Summary.Substring(0, 100) + "..." : s.Summary,
                            demoImageUrl = s.DemoImageUrl
                        })
                    })
                    .OrderByDescending(pa => pa.count)
                    .ToListAsync();

                return Ok(problemAreas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving detailed problem areas");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/solutions/list (for admin dropdowns)
        [HttpGet("list")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<IEnumerable<object>>> GetSolutionsList()
        {
            try
            {
                var solutions = await _context.Solutions
                    .Where(s => s.IsActive)
                    .Select(s => new
                    {
                        id = s.Id,
                        title = s.Title
                    })
                    .OrderBy(s => s.title)
                    .ToListAsync();

                return Ok(solutions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting solutions list");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/solutions (multipart/form-data)
        [HttpPost("upload")]
        [Authorize(Roles = "Admin,Manager")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(50 * 1024 * 1024)]
        public async Task<ActionResult<Solution>> CreateSolution([FromForm] SolutionFormDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                string uploadsDir = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "solutions");
                Directory.CreateDirectory(uploadsDir);

                string? demoImageUrl = await ProcessUploadedFile(dto.DemoImageFile, uploadsDir, "demo");

                var solution = new Solution
                {
                    Title = dto.Title,
                    Summary = dto.Summary,
                    ProblemArea = dto.ProblemArea,
                    DemoImageUrl = demoImageUrl,
                    DemoVideoUrl = dto.DemoVideoUrl,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Solutions.Add(solution);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetSolution), new { id = solution.Id }, solution);
            }
            catch (FileValidationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating solution");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/solutions/5 (supports both JSON and form-data)
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        [RequestSizeLimit(50 * 1024 * 1024)]
        public async Task<IActionResult> UpdateSolution([FromRoute] int id)
        {
            try
            {
                var existing = await _context.Solutions.FindAsync(id);
                if (existing == null || !existing.IsActive)
                    return NotFound();

                var contentType = Request.ContentType?.ToLowerInvariant() ?? "";
                _logger.LogInformation($"UpdateSolution called for ID {id}. Content-Type: {contentType}");

                // Handle multipart/form-data (with file upload)
                if (contentType.Contains("multipart/form-data"))
                {
                    _logger.LogInformation($"Processing form-data update for solution {id}");
                    
                    var form = await Request.ReadFormAsync();
                    
                    // Validate required fields
                    if (!form.ContainsKey("title") || !form.ContainsKey("summary") || !form.ContainsKey("problemArea"))
                    {
                        return BadRequest("Missing required fields: title, summary, problemArea");
                    }

                    existing.Title = form["title"].ToString();
                    existing.Summary = form["summary"].ToString();
                    existing.ProblemArea = form["problemArea"].ToString();
                    
                    if (form.ContainsKey("demoVideoUrl"))
                        existing.DemoVideoUrl = form["demoVideoUrl"].ToString();

                    // Handle file upload
                    if (form.Files.Any(f => f.Name == "demoImageFile"))
                    {
                        var file = form.Files.First(f => f.Name == "demoImageFile");
                        string uploadsDir = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "solutions");
                        Directory.CreateDirectory(uploadsDir);
                        existing.DemoImageUrl = await ProcessUploadedFile(file, uploadsDir, "demo");
                    }
                }
                // Handle JSON update
                else if (contentType.Contains("application/json"))
                {
                    _logger.LogInformation($"Processing JSON update for solution {id}");
                    
                    var jsonString = await new StreamReader(Request.Body).ReadToEndAsync();
                    var jsonDto = System.Text.Json.JsonSerializer.Deserialize<SolutionDto>(jsonString, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (jsonDto == null)
                    {
                        return BadRequest("Invalid JSON data");
                    }

                    // Validate that at least one field is provided for update
                    if (string.IsNullOrEmpty(jsonDto.Title) &&
                        string.IsNullOrEmpty(jsonDto.Summary) &&
                        string.IsNullOrEmpty(jsonDto.ProblemArea) &&
                        string.IsNullOrEmpty(jsonDto.DemoVideoUrl) &&
                        string.IsNullOrEmpty(jsonDto.DemoImageUrl))
                    {
                        _logger.LogWarning($"JSON update for solution {id} contains no valid fields");
                        return BadRequest("At least one field must be provided for update");
                    }

                    // Update only provided fields
                    if (!string.IsNullOrEmpty(jsonDto.Title))
                        existing.Title = jsonDto.Title;
                    if (!string.IsNullOrEmpty(jsonDto.Summary))
                        existing.Summary = jsonDto.Summary;
                    if (!string.IsNullOrEmpty(jsonDto.ProblemArea))
                        existing.ProblemArea = jsonDto.ProblemArea;
                    if (jsonDto.DemoVideoUrl != null)
                        existing.DemoVideoUrl = jsonDto.DemoVideoUrl;
                    if (!string.IsNullOrEmpty(jsonDto.DemoImageUrl))
                        existing.DemoImageUrl = jsonDto.DemoImageUrl;
                }
                else
                {
                    _logger.LogWarning($"Unsupported content type for solution {id}: {contentType}");
                    return BadRequest("Unsupported content type. Use application/json or multipart/form-data");
                }

                existing.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Solution {id} updated successfully");
                return NoContent();
            }
            catch (FileValidationException ex)
            {
                _logger.LogError(ex, $"File validation error updating solution {id}");
                return BadRequest(ex.Message);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, $"Concurrency conflict updating solution {id}");
                return Conflict("Concurrency conflict");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating solution {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: api/solutions/5
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeleteSolution([FromRoute] int id)
        {
            try
            {
                var solution = await _context.Solutions.FindAsync(id);
                if (solution == null || !solution.IsActive)
                    return NotFound();

                solution.IsActive = false;
                solution.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting solution {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        private async Task<string?> ProcessUploadedFile(IFormFile? file, string uploadsDir, string fileType)
        {
            if (file == null || file.Length == 0)
                return null;

            // Validate file
            if (file.Length > MaxFileSize)
                throw new FileValidationException($"{fileType} file size exceeds {MaxFileSize / 1024 / 1024}MB limit");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
                throw new FileValidationException($"Invalid {fileType} file type. Allowed: {string.Join(", ", AllowedExtensions)}");

            // Generate safe filename
            var fileName = $"{fileType}_{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsDir, fileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/solutions/{fileName}";
        }
    }
}
