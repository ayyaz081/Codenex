using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CodeNex.Data;
using CodeNex.DTOs;
using CodeNex.Models;
using System.Security.Claims;

namespace CodeNex.Controllers
{
    [Route("api/comments")]
    [ApiController]
    public class PublicCommentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PublicCommentsController> _logger;

        public PublicCommentsController(AppDbContext context, ILogger<PublicCommentsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/comments/publication/5
        [HttpGet("publication/{publicationId:int}")]
        public async Task<ActionResult<IEnumerable<CommentResponseDto>>> GetPublicationComments(int publicationId)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var comments = await _context.PublicationComments
                    .Include(c => c.User)
                    .Include(c => c.Likes)
                    .Where(c => c.PublicationId == publicationId && c.IsApproved)
                    .OrderByDescending(c => c.CreatedAt)
                    .AsNoTracking()
                    .Select(c => new CommentResponseDto
                    {
                        Id = c.Id,
                        PublicationId = c.PublicationId,
                        Content = c.Comment,
                        AuthorName = (c.User != null ? c.User.FirstName + " " + c.User.LastName : "Unknown Author"),
                        AuthorId = c.UserId,
                        CreatedAt = c.CreatedAt,
                        UpdatedAt = c.UpdatedAt,
                        IsApproved = c.IsApproved,
                        Rating = 0, // Comments don't have ratings in our system
                        LikesCount = c.Likes.Count,
                        IsLiked = currentUserId != null && c.Likes.Any(l => l.UserId == currentUserId)
                    })
                    .ToListAsync();

                return Ok(comments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving comments for publication {publicationId}");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/comments
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<CommentResponseDto>> CreateComment(CreateCommentDto createCommentDto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                // Check if publication exists
                var publicationExists = await _context.Publications
                    .AnyAsync(p => p.Id == createCommentDto.PublicationId);
                
                if (!publicationExists)
                    return NotFound("Publication not found");

                var comment = new PublicationComment
                {
                    PublicationId = createCommentDto.PublicationId,
                    UserId = userId,
                    Comment = createCommentDto.Content,
                    IsApproved = true, // Auto-approve for now, can be changed to false for moderation
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.PublicationComments.Add(comment);
                await _context.SaveChangesAsync();

                // Load the comment with user info for response
                var savedComment = await _context.PublicationComments
                    .Include(c => c.User)
                    .Include(c => c.Likes)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == comment.Id);

                if (savedComment == null)
                    return StatusCode(500, "Failed to retrieve saved comment");

                var responseDto = new CommentResponseDto
                {
                    Id = savedComment.Id,
                    PublicationId = savedComment.PublicationId,
                    Content = savedComment.Comment,
                    AuthorName = (savedComment.User != null ? savedComment.User.FirstName + " " + savedComment.User.LastName : "Unknown Author"),
                    AuthorId = savedComment.UserId,
                    CreatedAt = savedComment.CreatedAt,
                    UpdatedAt = savedComment.UpdatedAt,
                    IsApproved = savedComment.IsApproved,
                    Rating = 0,
                    LikesCount = 0,
                    IsLiked = false
                };

                return CreatedAtAction(nameof(GetPublicationComments), 
                    new { publicationId = comment.PublicationId }, responseDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating comment");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/comments/5/like
        [HttpPost("{commentId:int}/like")]
        [Authorize]
        public async Task<IActionResult> ToggleCommentLike(int commentId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                // Check if comment exists
                var commentExists = await _context.PublicationComments
                    .AnyAsync(c => c.Id == commentId);
                
                if (!commentExists)
                    return NotFound("Comment not found");

                var existingLike = await _context.CommentLikes
                    .FirstOrDefaultAsync(l => l.CommentId == commentId && l.UserId == userId);

                if (existingLike != null)
                {
                    // Unlike - remove the like
                    _context.CommentLikes.Remove(existingLike);
                }
                else
                {
                    // Like - add the like
                    var like = new CommentLike
                    {
                        CommentId = commentId,
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.CommentLikes.Add(like);
                }

                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error toggling like for comment {commentId}");
                return StatusCode(500, "Internal server error");
            }
        }


    }
}
