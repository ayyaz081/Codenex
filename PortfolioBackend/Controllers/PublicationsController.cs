using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortfolioBackend.Data;
using PortfolioBackend.Models;
using PortfolioBackend.DTOs;
using PortfolioBackend.Exceptions;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace PortfolioBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PublicationsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<PublicationsController> _logger;

        // File upload constraints
        private const long MaxFileSize = 10 * 1024 * 1024; // 10MB
        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
        private static readonly string[] AllowedDocumentExtensions = { ".pdf", ".doc", ".docx" };

        public PublicationsController(
            AppDbContext context,
            IWebHostEnvironment env,
            ILogger<PublicationsController> logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
        }

        // GET: api/publications
        [HttpGet]
        [ResponseCache(Duration = 60)]
        public async Task<ActionResult<IEnumerable<object>>> GetPublications()
        {
            try
            {
                _logger.LogInformation("Starting to retrieve publications");
                
                var publications = await _context.Publications
                    .Include(p => p.Ratings)
                    .Where(p => p.IsPublished)
                    .OrderByDescending(p => p.PublishedDate)
                    .AsNoTracking()
                    .ToListAsync();
                
                var result = publications.Select(p => new {
                    id = p.Id,
                    title = p.Title,
                    authors = p.Authors,
                    domain = p.Domain,
                    @abstract = p.Abstract,
                    thumbnailUrl = p.ThumbnailUrl,
                    downloadUrl = p.DownloadUrl,
                    keywords = p.Keywords,
                    isPublished = p.IsPublished,
                    publishedDate = p.PublishedDate,
                    createdAt = p.CreatedAt,
                    updatedAt = p.UpdatedAt,
                    // Calculate ratings from loaded navigation property
                    averageRating = p.Ratings.Any() ? p.Ratings.Average(r => (double)r.Rating) : 0.0,
                    ratingCount = p.Ratings.Count
                }).ToList();
                    
                _logger.LogInformation($"Retrieved {publications.Count} publications");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving publications: {Message}", ex.Message);
                _logger.LogError(ex, "Stack trace: {StackTrace}", ex.StackTrace);
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        // GET: api/publications/5
        [HttpGet("{id:int}")]
        [ResponseCache(Duration = 120)]
        public async Task<ActionResult<Publication>> GetPublication([FromRoute] int id)
        {
            try
            {
                var publication = await _context.Publications
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == id && p.IsPublished);

                return publication == null ? NotFound() : Ok(publication);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving publication with ID {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/publications/domain/{domain}
        [HttpGet("domain/{domain}")]
        [ResponseCache(Duration = 60, VaryByQueryKeys = new[] { "domain" })]
        public async Task<ActionResult<IEnumerable<Publication>>> GetByDomain([FromRoute] string domain)
        {
            try
            {
                return await _context.Publications
                    .Where(p => p.Domain == domain && p.IsPublished)
                    .OrderByDescending(p => p.PublishedDate)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving publications for domain {domain}");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/publications/search
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Publication>>> SearchPublications(
            [FromQuery] string? title,
            [FromQuery] string? author,
            [FromQuery] string? keywords)
        {
            try
            {
                var query = _context.Publications
                    .Where(p => p.IsPublished)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(title))
                    query = query.Where(p => p.Title.Contains(title));

                if (!string.IsNullOrEmpty(author))
                    query = query.Where(p => p.Authors.Contains(author));

                if (!string.IsNullOrEmpty(keywords))
                    query = query.Where(p => p.Keywords != null && p.Keywords.Contains(keywords));

                return await query
                    .OrderByDescending(p => p.PublishedDate)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching publications");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/publications/upload (multipart/form-data)
        [HttpPost("upload")]
        [Authorize(Roles = "Admin,Manager")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(20 * 1024 * 1024)] // 20MB for publications with documents
        public async Task<ActionResult<Publication>> CreatePublication([FromForm] PublicationUploadDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                string uploadsDir = Path.Combine(_env.WebRootPath ?? "wwwroot", "Uploads", "publications");
                Directory.CreateDirectory(uploadsDir);

                string? thumbnailUrl = null;
                string? downloadUrl = null;

                if (dto.ThumbnailFile != null)
                {
                    thumbnailUrl = await ProcessUploadedFile(dto.ThumbnailFile, uploadsDir, "thumbnail", AllowedImageExtensions);
                }

                if (dto.DocumentFile != null)
                {
                    downloadUrl = await ProcessUploadedFile(dto.DocumentFile, uploadsDir, "document", AllowedDocumentExtensions);
                }

                var publication = new Publication
                {
                    Title = dto.Title,
                    Authors = dto.Authors,
                    Domain = dto.Domain,
                    Abstract = dto.Abstract,
                    Keywords = dto.Keywords ?? string.Empty,
                    ThumbnailUrl = thumbnailUrl ?? string.Empty,
                    DownloadUrl = downloadUrl ?? string.Empty,
                    PublishedDate = dto.PublishedDate ?? DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsPublished = true
                };

                _context.Publications.Add(publication);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetPublication), new { id = publication.Id }, publication);
            }
            catch (FileValidationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating publication");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/publications/5
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UpdatePublication(
            [FromRoute] int id,
            [FromBody] PublicationDto dto)
        {
            try
            {
                var existing = await _context.Publications.FindAsync(id);
                if (existing == null)
                    return NotFound();

                // Update only provided fields
                if (!string.IsNullOrEmpty(dto.Title))
                    existing.Title = dto.Title;
                if (!string.IsNullOrEmpty(dto.Authors))
                    existing.Authors = dto.Authors;
                if (!string.IsNullOrEmpty(dto.Domain))
                    existing.Domain = dto.Domain;
                if (!string.IsNullOrEmpty(dto.Abstract))
                    existing.Abstract = dto.Abstract;
                if (!string.IsNullOrEmpty(dto.Keywords))
                    existing.Keywords = dto.Keywords;
                if (dto.PublishedDate.HasValue)
                    existing.PublishedDate = dto.PublishedDate.Value;
                if (dto.IsPublished.HasValue)
                    existing.IsPublished = dto.IsPublished.Value;

                existing.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating publication {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/publications/5/download
        [HttpGet("{id:int}/download")]
        [Authorize]
        public async Task<IActionResult> DownloadPublication([FromRoute] int id)
        {
            try
            {
                _logger.LogInformation($"Download request received for publication ID: {id}");
                
                var publication = await _context.Publications
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == id && p.IsPublished);

                if (publication == null)
                {
                    _logger.LogWarning($"Publication not found: {id}");
                    return NotFound("Publication not found");
                }

                if (string.IsNullOrEmpty(publication.DownloadUrl))
                {
                    _logger.LogWarning($"No download file available for publication {id}");
                    return NotFound("Download file not available");
                }

                // Construct the full file path
                var filePath = Path.Combine(_env.WebRootPath ?? "wwwroot", publication.DownloadUrl.TrimStart('/'));
                
                if (!System.IO.File.Exists(filePath))
                {
                    _logger.LogWarning($"File not found on disk: {filePath}");
                    return NotFound("Publication file not found");
                }

                var fileExtension = Path.GetExtension(filePath).ToLowerInvariant();
                var contentType = fileExtension switch
                {
                    ".pdf" => "application/pdf",
                    ".doc" => "application/msword",
                    ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    _ => "application/octet-stream"
                };

                var fileName = $"{publication.Title.Replace(" ", "_").Replace("/", "_")}{fileExtension}";
                
                _logger.LogInformation($"Serving download for publication {id}, filename: {fileName}");
                
                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error downloading publication {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: api/publications/5
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeletePublication([FromRoute] int id)
        {
            try
            {
                var publication = await _context.Publications.FindAsync(id);
                if (publication == null)
                    return NotFound();

                // Soft delete
                publication.IsPublished = false;
                publication.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting publication {id}");
                return StatusCode(500, "Internal server error");
            }
        }



        private async Task<string?> ProcessUploadedFile(IFormFile? file, string uploadsDir, string fileType, string[] allowedExtensions)
        {
            if (file == null || file.Length == 0)
                return null;

            // Validate file
            if (file.Length > MaxFileSize)
                throw new FileValidationException($"{fileType} file size exceeds {MaxFileSize / 1024 / 1024}MB limit");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
                throw new FileValidationException($"Invalid {fileType} file type. Allowed: {string.Join(", ", allowedExtensions)}");

            // Generate safe filename
            var fileName = $"{fileType}_{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsDir, fileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/Uploads/publications/{fileName}";
        }

    }
}
