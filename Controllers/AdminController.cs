using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CodeNex.Data;
using CodeNex.Models;
using System.Security.Claims;

namespace CodeNex.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly AppDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            UserManager<User> userManager,
            AppDbContext context,
            ILogger<AdminController> logger)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        // GET: api/admin/users
        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<object>>> GetUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] string? role = null)
        {
            try
            {
                var query = _userManager.Users.AsQueryable();

                // Apply search filter
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(u => u.FirstName.Contains(search) ||
                                           u.LastName.Contains(search) ||
                                           u.Email!.Contains(search));
                }

                // Apply role filter
                if (!string.IsNullOrEmpty(role))
                {
                    query = query.Where(u => u.Role == role);
                }

                var totalCount = await query.CountAsync();
                var users = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new
                    {
                        id = u.Id,
                        firstName = u.FirstName,
                        lastName = u.LastName,
                        email = u.Email,
                        role = u.Role,
                        isBlocked = u.IsBlocked,
                        emailVerified = u.EmailConfirmed,
                        lastLoginDate = u.LastLoginDate,
                        createdAt = u.CreatedAt
                    })
                    .ToListAsync();

                return Ok(new
                {
                    users,
                    pagination = new
                    {
                        page,
                        pageSize,
                        totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return StatusCode(500, new { message = "An error occurred while retrieving users." });
            }
        }

        // GET: api/admin/users/{id}
        [HttpGet("users/{id}")]
        public async Task<ActionResult<object>> GetUser([FromRoute] string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                    return NotFound(new { message = "User not found." });

                var userInfo = new
                {
                    id = user.Id,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    email = user.Email,
                    role = user.Role,
                    isBlocked = user.IsBlocked,
                    emailVerified = user.EmailConfirmed,
                    lastLoginDate = user.LastLoginDate,
                    createdAt = user.CreatedAt,
                    updatedAt = user.UpdatedAt
                };

                return Ok(userInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving user {id}");
                return StatusCode(500, new { message = "An error occurred while retrieving user." });
            }
        }

        // PUT: api/admin/users/{id}/role
        [HttpPut("users/{id}/role")]
        public async Task<IActionResult> UpdateUserRole([FromRoute] string id, [FromBody] UpdateUserRoleDto dto)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                    return NotFound(new { message = "User not found." });

                // Prevent admin from changing their own role
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (currentUserId == id)
                    return BadRequest(new { message = "You cannot change your own role." });

                // Validate role
                var validRoles = new[] { "Admin", "Manager", "RegisteredUser", "Guest" };
                if (!validRoles.Contains(dto.Role))
                    return BadRequest(new { message = "Invalid role specified." });

                user.Role = dto.Role;
                user.UpdatedAt = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                    return BadRequest(new { message = "Failed to update user role.", errors = result.Errors });

                _logger.LogInformation("User role updated: {UserId} changed to {Role} by {AdminId}", 
                    user.Id, dto.Role, currentUserId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating role for user {id}");
                return StatusCode(500, new { message = "An error occurred while updating user role." });
            }
        }

        // PUT: api/admin/users/{id}/block
        [HttpPut("users/{id}/block")]
        public async Task<IActionResult> BlockUser([FromRoute] string id, [FromBody] BlockUserDto dto)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                    return NotFound(new { message = "User not found." });

                // Prevent admin from blocking themselves
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (currentUserId == id)
                    return BadRequest(new { message = "You cannot block yourself." });

                user.IsBlocked = dto.IsBlocked;
                user.UpdatedAt = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                    return BadRequest(new { message = "Failed to update user block status.", errors = result.Errors });

                _logger.LogInformation("User block status updated: {UserId} {Action} by {AdminId}", 
                    user.Id, dto.IsBlocked ? "blocked" : "unblocked", currentUserId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating block status for user {id}");
                return StatusCode(500, new { message = "An error occurred while updating user block status." });
            }
        }

        // DELETE: api/admin/users/{id}
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser([FromRoute] string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                    return NotFound(new { message = "User not found." });

                // Prevent admin from deleting themselves
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (currentUserId == id)
                    return BadRequest(new { message = "You cannot delete yourself." });

                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                    return BadRequest(new { message = "Failed to delete user.", errors = result.Errors });

                _logger.LogInformation("User deleted: {UserId} by {AdminId}", user.Id, currentUserId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting user {id}");
                return StatusCode(500, new { message = "An error occurred while deleting user." });
            }
        }

        // GET: api/admin/stats
        [HttpGet("stats")]
        public async Task<ActionResult<object>> GetDashboardStats()
        {
            try
            {
                var totalUsers = await _userManager.Users.CountAsync();
                var totalAdmins = await _userManager.Users.CountAsync(u => u.Role == "Admin");
                var totalRegisteredUsers = await _userManager.Users.CountAsync(u => u.Role == "RegisteredUser");
                var blockedUsers = await _userManager.Users.CountAsync(u => u.IsBlocked);
                
                var totalProducts = await _context.Products.CountAsync();
                var totalPublications = await _context.Publications.CountAsync(p => p.IsPublished);
                var totalRepositories = await _context.Repositories.CountAsync(r => r.IsActive);
                var totalSolutions = await _context.Solutions.CountAsync();

                // Recent activity
                var recentUsers = await _userManager.Users
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(5)
                    .Select(u => new
                    {
                        id = u.Id,
                        name = $"{u.FirstName} {u.LastName}",
                        email = u.Email,
                        role = u.Role,
                        createdAt = u.CreatedAt
                    })
                    .ToListAsync();

                return Ok(new
                {
                    userStats = new
                    {
                        totalUsers,
                        totalAdmins,
                        totalRegisteredUsers,
                        blockedUsers
                    },
                    contentStats = new
                    {
                        totalProducts,
                        totalPublications,
                        totalRepositories,
                        totalSolutions
                    },
                    recentUsers
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dashboard stats");
                return StatusCode(500, new { message = "An error occurred while retrieving dashboard statistics." });
            }
        }

    }

    // DTOs for admin operations
    public class UpdateUserRoleDto
    {
        public string Role { get; set; } = string.Empty;
    }

    public class BlockUserDto
    {
        public bool IsBlocked { get; set; }
    }
}
