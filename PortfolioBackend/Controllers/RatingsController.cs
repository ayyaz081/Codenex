using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortfolioBackend.Data;
using PortfolioBackend.DTOs;
using PortfolioBackend.Models;
using System.Security.Claims;

namespace PortfolioBackend.Controllers
{
    [Route("api/ratings")]
    [ApiController]
    public class RatingsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<RatingsController> _logger;

        public RatingsController(AppDbContext context, ILogger<RatingsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/ratings/user/5 - Get user's rating for a specific publication
        [HttpGet("user/{publicationId:int}")]
        [Authorize]
        public async Task<ActionResult<RatingResponseDto>> GetUserRating(int publicationId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var rating = await _context.PublicationRatings
                    .Where(r => r.PublicationId == publicationId && r.UserId == userId)
                    .AsNoTracking()
                    .Select(r => new RatingResponseDto
                    {
                        Id = r.Id,
                        PublicationId = r.PublicationId,
                        Rating = r.Rating,
                        UserId = r.UserId,
                        CreatedAt = r.CreatedAt,
                        UpdatedAt = r.UpdatedAt
                    })
                    .FirstOrDefaultAsync();

                if (rating == null)
                    return NotFound("User rating not found");

                return Ok(rating);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving user rating for publication {publicationId}");
                return StatusCode(500, "Internal server error");
            }
        }


        // POST: api/ratings
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<RatingResponseDto>> CreateRating(CreateRatingDto createRatingDto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                // Check if publication exists
                var publicationExists = await _context.Publications
                    .AnyAsync(p => p.Id == createRatingDto.PublicationId);
                
                if (!publicationExists)
                    return NotFound("Publication not found");

                // Check if user has already rated this publication
                var existingRating = await _context.PublicationRatings
                    .FirstOrDefaultAsync(r => r.PublicationId == createRatingDto.PublicationId && r.UserId == userId);

                if (existingRating != null)
                    return Conflict("User has already rated this publication. Use PUT to update.");

                var rating = new PublicationRating
                {
                    PublicationId = createRatingDto.PublicationId,
                    UserId = userId,
                    Rating = createRatingDto.Rating,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.PublicationRatings.Add(rating);
                await _context.SaveChangesAsync();

                var responseDto = new RatingResponseDto
                {
                    Id = rating.Id,
                    PublicationId = rating.PublicationId,
                    Rating = rating.Rating,
                    UserId = rating.UserId,
                    CreatedAt = rating.CreatedAt,
                    UpdatedAt = rating.UpdatedAt
                };

                return CreatedAtAction(nameof(GetUserRating), 
                    new { publicationId = rating.PublicationId }, responseDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating rating");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/ratings/5
        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<ActionResult<RatingResponseDto>> UpdateRating(int id, UpdateRatingDto updateRatingDto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var rating = await _context.PublicationRatings
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (rating == null)
                    return NotFound();

                // Only allow the rating author to update their rating
                if (rating.UserId != userId)
                    return Forbid();

                rating.Rating = updateRatingDto.Rating;
                rating.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var responseDto = new RatingResponseDto
                {
                    Id = rating.Id,
                    PublicationId = rating.PublicationId,
                    Rating = rating.Rating,
                    UserId = rating.UserId,
                    CreatedAt = rating.CreatedAt,
                    UpdatedAt = rating.UpdatedAt
                };

                return Ok(responseDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating rating {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/ratings/publication/5 - Get all ratings for a specific publication
        [HttpGet("publication/{publicationId:int}")]
        public async Task<ActionResult<IEnumerable<object>>> GetPublicationRatings(int publicationId)
        {
            try
            {
                var ratings = await _context.PublicationRatings
                    .Where(r => r.PublicationId == publicationId)
                    .Include(r => r.User)
                    .AsNoTracking()
                    .Select(r => new {
                        id = r.Id,
                        publicationId = r.PublicationId,
                        rating = r.Rating,
                        createdAt = r.CreatedAt,
                        user = new {
                            id = r.User != null ? r.User.Id : string.Empty,
                        firstName = r.User != null ? r.User.FirstName : "Unknown",
                        lastName = r.User != null ? r.User.LastName : "User"
                        }
                    })
                    .ToListAsync();

                return Ok(ratings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving ratings for publication {publicationId}");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/ratings/publication/5/average - Get average rating for a specific publication
        [HttpGet("publication/{publicationId:int}/average")]
        public async Task<ActionResult<object>> GetAverageRating(int publicationId)
        {
            try
            {
                var ratings = await _context.PublicationRatings
                    .Where(r => r.PublicationId == publicationId)
                    .AsNoTracking()
                    .ToListAsync();

                if (ratings.Count == 0)
                {
                    return Ok(new { 
                        publicationId = publicationId,
                        averageRating = 0.0,
                        totalRatings = 0 
                    });
                }

                var averageRating = ratings.Average(r => r.Rating);
                
                return Ok(new {
                    publicationId = publicationId,
                    averageRating = Math.Round(averageRating, 1),
                    totalRatings = ratings.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error calculating average rating for publication {publicationId}");
                return StatusCode(500, "Internal server error");
            }
        }


    }
}
