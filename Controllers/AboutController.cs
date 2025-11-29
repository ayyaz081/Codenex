using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Neelsol.Data;
using Neelsol.Models;
using Neelsol.Filters;

namespace Neelsol.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AboutController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<AboutController> _logger;

        public AboutController(AppDbContext context, IWebHostEnvironment environment, ILogger<AboutController> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        // ==================== TEAM MEMBERS ENDPOINTS ====================

        // GET: api/About/team
        [HttpGet("team")]
        [NoCache]
        public async Task<ActionResult<IEnumerable<TeamMember>>> GetTeamMembers()
        {
            try
            {
                var teamMembers = await _context.TeamMembers
                    .OrderBy(tm => tm.DisplayOrder)
                    .ToListAsync();

                return Ok(teamMembers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching team members");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/About/team/{id}
        [HttpGet("team/{id}")]
        [NoCache]
        public async Task<ActionResult<TeamMember>> GetTeamMember(int id)
        {
            try
            {
                var teamMember = await _context.TeamMembers.FindAsync(id);

                if (teamMember == null)
                {
                    return NotFound($"Team member with ID {id} not found");
                }

                return Ok(teamMember);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching team member {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/About/team
        [HttpPost("team")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<TeamMember>> CreateTeamMember([FromForm] TeamMemberDto dto, IFormFile? photoFile)
        {
            try
            {
                var teamMember = new TeamMember
                {
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Position = dto.Position,
                    Department = dto.Department,
                    Email = dto.Email,
                    Bio = dto.Bio,
                    LinkedInUrl = dto.LinkedInUrl,
                    TwitterUrl = dto.TwitterUrl,
                    DisplayOrder = dto.DisplayOrder ?? 999,
                    IsActive = dto.IsActive ?? true,
                    CreatedAt = DateTime.UtcNow
                };

                // Handle photo upload
                if (photoFile != null && photoFile.Length > 0)
                {
                    var photoPath = await SaveFileAsync(photoFile, "team");
                    teamMember.PhotoUrl = photoPath;
                }

                _context.TeamMembers.Add(teamMember);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetTeamMember), new { id = teamMember.Id }, teamMember);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating team member");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // PUT: api/About/team/{id}
        [HttpPut("team/{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UpdateTeamMember(int id, [FromForm] TeamMemberDto dto, IFormFile? photoFile)
        {
            try
            {
                var teamMember = await _context.TeamMembers.FindAsync(id);
                if (teamMember == null)
                {
                    return NotFound($"Team member with ID {id} not found");
                }

                // Update properties
                teamMember.FirstName = dto.FirstName;
                teamMember.LastName = dto.LastName;
                teamMember.Position = dto.Position;
                teamMember.Department = dto.Department;
                teamMember.Email = dto.Email;
                teamMember.Bio = dto.Bio;
                teamMember.LinkedInUrl = dto.LinkedInUrl;
                teamMember.TwitterUrl = dto.TwitterUrl;
                teamMember.DisplayOrder = dto.DisplayOrder ?? teamMember.DisplayOrder;
                teamMember.IsActive = dto.IsActive ?? teamMember.IsActive;
                teamMember.UpdatedAt = DateTime.UtcNow;

                // Handle photo upload
                if (photoFile != null && photoFile.Length > 0)
                {
                    // Delete old photo if exists
                    if (!string.IsNullOrEmpty(teamMember.PhotoUrl))
                    {
                        DeleteFile(teamMember.PhotoUrl);
                    }

                    var photoPath = await SaveFileAsync(photoFile, "team");
                    teamMember.PhotoUrl = photoPath;
                }

                await _context.SaveChangesAsync();

                return Ok(teamMember);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating team member {id}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // DELETE: api/About/team/{id}
        [HttpDelete("team/{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeleteTeamMember(int id)
        {
            try
            {
                var teamMember = await _context.TeamMembers.FindAsync(id);
                if (teamMember == null)
                {
                    return NotFound($"Team member with ID {id} not found");
                }

                // Delete photo if exists
                if (!string.IsNullOrEmpty(teamMember.PhotoUrl))
                {
                    DeleteFile(teamMember.PhotoUrl);
                }

                _context.TeamMembers.Remove(teamMember);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting team member {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        // ==================== TESTIMONIALS ENDPOINTS ====================

        // GET: api/About/testimonials
        [HttpGet("testimonials")]
        [NoCache]
        public async Task<ActionResult<IEnumerable<ClientTestimonial>>> GetTestimonials()
        {
            try
            {
                var testimonials = await _context.ClientTestimonials
                    .OrderBy(t => t.DisplayOrder)
                    .ToListAsync();

                return Ok(testimonials);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching testimonials");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/About/testimonials/{id}
        [HttpGet("testimonials/{id}")]
        [NoCache]
        public async Task<ActionResult<ClientTestimonial>> GetTestimonial(int id)
        {
            try
            {
                var testimonial = await _context.ClientTestimonials.FindAsync(id);

                if (testimonial == null)
                {
                    return NotFound($"Testimonial with ID {id} not found");
                }

                return Ok(testimonial);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching testimonial {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/About/testimonials
        [HttpPost("testimonials")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<ClientTestimonial>> CreateTestimonial([FromForm] TestimonialDto dto, IFormFile? clientPhotoFile)
        {
            try
            {
                var testimonial = new ClientTestimonial
                {
                    ClientName = dto.ClientName,
                    ClientPosition = dto.ClientPosition,
                    CompanyName = dto.CompanyName,
                    Message = dto.Message,
                    Rating = dto.Rating ?? 5,
                    DisplayOrder = dto.DisplayOrder ?? 999,
                    IsActive = dto.IsActive ?? true,
                    IsApproved = dto.IsApproved ?? true,
                    CreatedAt = DateTime.UtcNow
                };

                // Handle client photo upload
                if (clientPhotoFile != null && clientPhotoFile.Length > 0)
                {
                    var photoPath = await SaveFileAsync(clientPhotoFile, "testimonials");
                    testimonial.ClientPhotoUrl = photoPath;
                }

                _context.ClientTestimonials.Add(testimonial);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetTestimonial), new { id = testimonial.Id }, testimonial);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating testimonial");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // PUT: api/About/testimonials/{id}
        [HttpPut("testimonials/{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UpdateTestimonial(int id, [FromForm] TestimonialDto dto, IFormFile? clientPhotoFile)
        {
            try
            {
                var testimonial = await _context.ClientTestimonials.FindAsync(id);
                if (testimonial == null)
                {
                    return NotFound($"Testimonial with ID {id} not found");
                }

                // Update properties
                testimonial.ClientName = dto.ClientName;
                testimonial.ClientPosition = dto.ClientPosition;
                testimonial.CompanyName = dto.CompanyName;
                testimonial.Message = dto.Message;
                testimonial.Rating = dto.Rating ?? testimonial.Rating;
                testimonial.DisplayOrder = dto.DisplayOrder ?? testimonial.DisplayOrder;
                testimonial.IsActive = dto.IsActive ?? testimonial.IsActive;
                testimonial.IsApproved = dto.IsApproved ?? testimonial.IsApproved;
                testimonial.UpdatedAt = DateTime.UtcNow;

                // Handle client photo upload
                if (clientPhotoFile != null && clientPhotoFile.Length > 0)
                {
                    // Delete old photo if exists
                    if (!string.IsNullOrEmpty(testimonial.ClientPhotoUrl))
                    {
                        DeleteFile(testimonial.ClientPhotoUrl);
                    }

                    var photoPath = await SaveFileAsync(clientPhotoFile, "testimonials");
                    testimonial.ClientPhotoUrl = photoPath;
                }

                await _context.SaveChangesAsync();

                return Ok(testimonial);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating testimonial {id}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // DELETE: api/About/testimonials/{id}
        [HttpDelete("testimonials/{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeleteTestimonial(int id)
        {
            try
            {
                var testimonial = await _context.ClientTestimonials.FindAsync(id);
                if (testimonial == null)
                {
                    return NotFound($"Testimonial with ID {id} not found");
                }

                // Delete photo if exists
                if (!string.IsNullOrEmpty(testimonial.ClientPhotoUrl))
                {
                    DeleteFile(testimonial.ClientPhotoUrl);
                }

                _context.ClientTestimonials.Remove(testimonial);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting testimonial {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        // ==================== HELPER METHODS ====================

        private async Task<string> SaveFileAsync(IFormFile file, string subfolder)
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", subfolder);
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/{subfolder}/{uniqueFileName}";
        }

        private void DeleteFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;

            try
            {
                var fullPath = Path.Combine(_environment.WebRootPath, filePath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Could not delete file: {filePath}");
            }
        }
    }

    // DTOs for request handling
    public class TeamMemberDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string? Department { get; set; }
        public string? Email { get; set; }
        public string? Bio { get; set; }
        public string? LinkedInUrl { get; set; }
        public string? TwitterUrl { get; set; }
        public int? DisplayOrder { get; set; }
        public bool? IsActive { get; set; }
    }

    public class TestimonialDto
    {
        public string ClientName { get; set; } = string.Empty;
        public string ClientPosition { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int? Rating { get; set; }
        public int? DisplayOrder { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsApproved { get; set; }
    }
}
