using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortfolioBackend.Data;
using PortfolioBackend.Models;
using System.Security.Claims;

namespace PortfolioBackend.Controllers
{
    [Route("api/admin/comments")]
    [ApiController]
    [Authorize(Roles = "Admin,Manager")]
    public class AdminCommentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AdminCommentsController> _logger;

        public AdminCommentsController(AppDbContext context, ILogger<AdminCommentsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/comments
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetComments(
            [FromQuery] bool? isApproved = null,
            [FromQuery] int? publicationId = null)
        {
            try
            {
                var query = _context.PublicationComments
                    .Include(c => c.User)
                    .Include(c => c.Publication)
                    .AsQueryable();

                if (isApproved.HasValue)
                    query = query.Where(c => c.IsApproved == isApproved.Value);

                if (publicationId.HasValue)
                    query = query.Where(c => c.PublicationId == publicationId.Value);

                var comments = await query
                    .OrderByDescending(c => c.CreatedAt)
                    .AsNoTracking()
                    .Select(c => new {
                        id = c.Id,
                        comment = c.Comment,
                        isApproved = c.IsApproved,
                        createdAt = c.CreatedAt,
                        updatedAt = c.UpdatedAt,
                        publicationId = c.PublicationId,
                        user = c.User != null ? new {
                            id = c.User.Id,
                            firstName = c.User.FirstName,
                            lastName = c.User.LastName,
                            email = c.User.Email
                        } : null,
                        publication = c.Publication != null ? new {
                            id = c.Publication.Id,
                            title = c.Publication.Title,
                            domain = c.Publication.Domain
                        } : null
                    })
                    .ToListAsync();

                return Ok(comments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving comments");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/comments/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<object>> GetComment(int id)
        {
            try
            {
                var comment = await _context.PublicationComments
                    .Include(c => c.User)
                    .Include(c => c.Publication)
                    .AsNoTracking()
                    .Where(c => c.Id == id)
                    .Select(c => new {
                        id = c.Id,
                        comment = c.Comment,
                        isApproved = c.IsApproved,
                        createdAt = c.CreatedAt,
                        updatedAt = c.UpdatedAt,
                        publicationId = c.PublicationId,
                        user = c.User != null ? new {
                            id = c.User.Id,
                            firstName = c.User.FirstName,
                            lastName = c.User.LastName,
                            email = c.User.Email
                        } : null,
                        publication = c.Publication != null ? new {
                            id = c.Publication.Id,
                            title = c.Publication.Title,
                            domain = c.Publication.Domain
                        } : null
                    })
                    .FirstOrDefaultAsync();

                return comment == null ? NotFound() : Ok(comment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving comment with ID {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/comments/5/approve
        [HttpPut("{id:int}/approve")]
        public async Task<IActionResult> ApproveComment(int id)
        {
            try
            {
                var comment = await _context.PublicationComments.FindAsync(id);
                if (comment == null)
                    return NotFound();

                comment.IsApproved = true;
                comment.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error approving comment {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/comments/5/reject
        [HttpPut("{id:int}/reject")]
        public async Task<IActionResult> RejectComment(int id)
        {
            try
            {
                var comment = await _context.PublicationComments.FindAsync(id);
                if (comment == null)
                    return NotFound();

                comment.IsApproved = false;
                comment.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error rejecting comment {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: api/comments/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteComment(int id)
        {
            try
            {
                var comment = await _context.PublicationComments.FindAsync(id);
                if (comment == null)
                    return NotFound();

                _context.PublicationComments.Remove(comment);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting comment {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/comments/bulk-approve
        [HttpPut("bulk-approve")]
        public async Task<IActionResult> BulkApproveComments([FromBody] int[] commentIds)
        {
            try
            {
                var comments = await _context.PublicationComments
                    .Where(c => commentIds.Contains(c.Id))
                    .ToListAsync();

                foreach (var comment in comments)
                {
                    comment.IsApproved = true;
                    comment.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return Ok(new { approvedCount = comments.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk approving comments");
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: api/comments/bulk-delete
        [HttpDelete("bulk-delete")]
        public async Task<IActionResult> BulkDeleteComments([FromBody] int[] commentIds)
        {
            try
            {
                var comments = await _context.PublicationComments
                    .Where(c => commentIds.Contains(c.Id))
                    .ToListAsync();

                _context.PublicationComments.RemoveRange(comments);
                await _context.SaveChangesAsync();

                return Ok(new { deletedCount = comments.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk deleting comments");
                return StatusCode(500, "Internal server error");
            }
        }

    }
}
