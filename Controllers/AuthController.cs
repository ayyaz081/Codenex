using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Neelsol.DTOs;
using Neelsol.Models;
using Neelsol.Services;
using System.Security.Claims;

namespace Neelsol.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly TokenService _tokenService;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthController> _logger;
        private readonly IConfiguration _configuration;
        private readonly Neelsol.Data.AppDbContext _context;

        public AuthController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            TokenService tokenService,
            IEmailService emailService,
            ILogger<AuthController> logger,
            IConfiguration configuration,
            Neelsol.Data.AppDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _emailService = emailService;
            _logger = logger;
            _configuration = configuration;
            _context = context;
        }

        [HttpPost("register")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
                if (existingUser != null)
                {
                    return BadRequest(new { message = "User with this email already exists." });
                }

                // Create new user
                var user = new User
                {
                    UserName = registerDto.Email,
                    Email = registerDto.Email,
                    FirstName = registerDto.FirstName,
                    LastName = registerDto.LastName,
                    Role = registerDto.Role ?? "User", // Use role from DTO or default
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, registerDto.Password);

                if (!result.Succeeded)
                {
                    return BadRequest(new { message = "Registration failed.", errors = result.Errors });
                }

                // Generate email confirmation token
                var emailConfirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                
                // Create verification URL - configurable for different frontend deployments
                var baseUrl = Environment.GetEnvironmentVariable("FRONTEND_BASE_URL") ?? 
                             $"{Request.Scheme}://{Request.Host}";
                var verificationPath = Environment.GetEnvironmentVariable("EMAIL_VERIFICATION_PATH") ?? "/auth/verify";
                var verificationUrl = $"{baseUrl.TrimEnd('/')}{verificationPath}?userId={user.Id}&token={Uri.EscapeDataString(emailConfirmationToken)}";
                
                // Send verification email
                var emailSent = await _emailService.SendEmailVerificationAsync(user.Email!, user.FirstName, verificationUrl);
                
                if (emailSent)
                {
                    _logger.LogInformation("Email verification sent to user: {Email}", user.Email);
                }
                else
                {
                    _logger.LogWarning("Failed to send email verification to user: {Email}", user.Email);
                }

                // Generate JWT token for immediate login
                var token = _tokenService.CreateToken(user);

                var response = new AuthResponseDto
                {
                    Token = token,
                    Email = user.Email ?? string.Empty,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role,
                    UserId = user.Id,
                    EmailVerified = user.EmailConfirmed,
                    ExpiresAt = DateTime.UtcNow.AddHours(_tokenService.GetExpiryHours())
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration");
                return StatusCode(500, new { message = "An error occurred during registration." });
            }
        }

        [HttpPost("login")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var user = await _userManager.FindByEmailAsync(loginDto.Email);
                if (user == null)
                {
                    return Unauthorized(new { message = "Invalid email or password." });
                }

                if (user.IsBlocked)
                {
                    return Unauthorized(new { message = "Your account has been blocked. Please contact support." });
                }

                var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);
                if (!result.Succeeded)
                {
                    return Unauthorized(new { message = "Invalid email or password." });
                }

                // Update last login date
                user.LastLoginDate = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                // Generate JWT token
                var token = _tokenService.CreateToken(user);

                var response = new AuthResponseDto
                {
                    Token = token,
                    Email = user.Email ?? string.Empty,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role,
                    UserId = user.Id,
                    EmailVerified = user.EmailConfirmed,
                    ExpiresAt = DateTime.UtcNow.AddHours(_tokenService.GetExpiryHours())
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user login");
                return StatusCode(500, new { message = "An error occurred during login." });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                await _signInManager.SignOutAsync();
                return Ok(new { message = "Logged out successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, new { message = "An error occurred during logout." });
            }
        }

        [HttpPost("forgot-password")]
        [EnableRateLimiting("password-reset")]
        public async Task<IActionResult> ForgotPassword([FromBody] EmailDto forgotPasswordDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var user = await _userManager.FindByEmailAsync(forgotPasswordDto.Email);
                if (user == null)
                {
                    // Don't reveal that the user does not exist
                    return Ok(new { message = "If an account with that email exists, a password reset link has been sent." });
                }

                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                
                // Construct the reset URL pointing to frontend page with auto-populated token
                var baseUrl = Environment.GetEnvironmentVariable("FRONTEND_BASE_URL") ?? 
                             $"{Request.Scheme}://{Request.Host}";
                var resetPath = Environment.GetEnvironmentVariable("PASSWORD_RESET_PATH") ?? "/Auth";
                var resetUrl = $"{baseUrl.TrimEnd('/')}{resetPath}?action=reset&userId={user.Id}&token={Uri.EscapeDataString(resetToken)}";
                
                // Send password reset email
                var emailSent = await _emailService.SendPasswordResetAsync(user.Email!, user.FirstName, resetUrl);
                
                if (emailSent)
                {
                    _logger.LogInformation("Password reset email sent to user: {Email}", user.Email);
                }
                else
                {
                    _logger.LogWarning("Failed to send password reset email to user: {Email}", user.Email);
                }

                return Ok(new { message = "If an account with that email exists, a password reset link has been sent." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset request");
                return StatusCode(500, new { message = "An error occurred while processing the request." });
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var user = await _userManager.FindByIdAsync(resetPasswordDto.UserId);
                if (user == null)
                {
                    // For security reasons, don't reveal that the user does not exist
                    return Ok(new { message = "Password has been reset successfully." });
                }

                var result = await _userManager.ResetPasswordAsync(user, resetPasswordDto.Token, resetPasswordDto.NewPassword);
                if (!result.Succeeded)
                {
                    // For detailed error in development mode only
                    var errors = result.Errors.Select(e => e.Description).ToArray();
                    _logger.LogWarning("Password reset failed for user {UserId}: {Errors}",
                        resetPasswordDto.UserId, string.Join(", ", errors));
                    
                    return BadRequest(new { message = "Failed to reset password. The link may be invalid or expired." });
                }

                // Update the user's last updated timestamp
                user.UpdatedAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
                
                _logger.LogInformation("Password reset successful for user {Email}", user.Email);
                return Ok(new { message = "Password has been reset successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset");
                return StatusCode(500, new { message = "An error occurred while resetting the password." });
            }
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found." });
                }

                var profileInfo = new
                {
                    userId = user.Id,
                    email = user.Email,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    role = user.Role,
                    emailVerified = user.EmailConfirmed,
                    isBlocked = user.IsBlocked,
                    lastLoginDate = user.LastLoginDate,
                    createdAt = user.CreatedAt
                };

                return Ok(profileInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user profile");
                return StatusCode(500, new { message = "An error occurred while retrieving profile." });
            }
        }

        [HttpPost("create-admin")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> CreateAdmin(
            [FromBody] CreateAdminDto createAdminDto,
            [FromHeader(Name = "X-Admin-Secret")] string? adminSecret)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                
                // SECURITY: Require admin secret for admin creation
                // This prevents unauthorized admin account creation
                var requiredSecret = Environment.GetEnvironmentVariable("ADMIN_CREATION_SECRET") ??
                                   _configuration["AdminCreationSecret"];
                
                if (string.IsNullOrEmpty(requiredSecret))
                {
                    _logger.LogError("Admin creation attempted but ADMIN_CREATION_SECRET is not configured");
                    return StatusCode(500, new { message = "Admin creation is not properly configured." });
                }
                
                if (string.IsNullOrEmpty(adminSecret) || adminSecret != requiredSecret)
                {
                    _logger.LogWarning("Unauthorized admin creation attempt from IP: {IP}", 
                        HttpContext.Connection.RemoteIpAddress);
                    return Unauthorized(new { message = "Invalid admin creation credentials." });
                }

                // Check if any admin already exists
                var existingAdmin = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.Role == "Admin");
                
                if (existingAdmin != null)
                {
                    return BadRequest(new { message = "An admin user already exists. Use the admin panel to create additional admins." });
                }

                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(createAdminDto.Email);
                if (existingUser != null)
                {
                    return BadRequest(new { message = "User with this email already exists." });
                }

                // Create admin user
                var adminUser = new User
                {
                    UserName = createAdminDto.Email,
                    Email = createAdminDto.Email,
                    FirstName = createAdminDto.FirstName,
                    LastName = createAdminDto.LastName,
                    Role = "Admin",
                    EmailConfirmed = true, // Auto-confirm admin email
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(adminUser, createAdminDto.Password);

                if (!result.Succeeded)
                {
                    return BadRequest(new { message = "Failed to create admin user.", errors = result.Errors });
                }

                _logger.LogInformation("Admin user created successfully: {Email}", adminUser.Email);

                // Generate JWT token for immediate login
                var token = _tokenService.CreateToken(adminUser);

                var response = new AuthResponseDto
                {
                    Token = token,
                    Email = adminUser.Email ?? string.Empty,
                    FirstName = adminUser.FirstName,
                    LastName = adminUser.LastName,
                    Role = adminUser.Role,
                    UserId = adminUser.Id,
                    EmailVerified = adminUser.EmailConfirmed,
                    ExpiresAt = DateTime.UtcNow.AddHours(_tokenService.GetExpiryHours())
                };

                return Ok(new { 
                    message = "Admin user created successfully!", 
                    admin = response 
                });
            }
            catch (Exception ex)
            {
            _logger.LogError(ex, "Error creating admin user");
                return StatusCode(500, new { message = "An error occurred while creating admin user." });
            }
        }

        [HttpPost("resend-verification")]
        [EnableRateLimiting("password-reset")]
        public async Task<IActionResult> ResendVerificationEmail([FromBody] EmailDto resendDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var user = await _userManager.FindByEmailAsync(resendDto.Email);
                if (user == null)
                {
                    // Don't reveal that the user doesn't exist - but add delay to prevent enumeration
                    await Task.Delay(1000);
                    return Ok(new { message = "If an account with that email exists, a verification email has been sent." });
                }

                if (user.EmailConfirmed)
                {
                    return BadRequest(new { message = "Email is already verified." });
                }

                // Check if user was created recently (prevent spam for new accounts)
                if (user.CreatedAt > DateTime.UtcNow.AddMinutes(-2))
                {
                    return BadRequest(new { message = "Please wait a moment before requesting another verification email." });
                }

                // Generate new email confirmation token
                var emailConfirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                
                // Create verification URL - configurable for different frontend deployments
                var baseUrl = Environment.GetEnvironmentVariable("FRONTEND_BASE_URL") ?? 
                             $"{Request.Scheme}://{Request.Host}";
                var verificationPath = Environment.GetEnvironmentVariable("EMAIL_VERIFICATION_PATH") ?? "/auth/verify";
                var verificationUrl = $"{baseUrl.TrimEnd('/')}{verificationPath}?userId={user.Id}&token={Uri.EscapeDataString(emailConfirmationToken)}";
                
                // Send verification email
                var emailSent = await _emailService.SendEmailVerificationAsync(user.Email!, user.FirstName, verificationUrl);
                
                if (emailSent)
                {
                    _logger.LogInformation("Verification email resent to user: {Email}", user.Email);
                }
                else
                {
                    _logger.LogWarning("Failed to resend verification email to user: {Email}", user.Email);
                }

                return Ok(new { message = "If an account with that email exists, a verification email has been sent." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during resend email verification");
                return StatusCode(500, new { message = "An error occurred while resending verification email." });
            }
        }

        [HttpGet("verify-email")]
        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromQuery] string userId, [FromQuery] string token)
        {
            try
            {
                _logger.LogInformation("Email verification attempt - UserId: {UserId}, Token length: {TokenLength}", 
                    userId ?? "null", token?.Length ?? 0);

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("Invalid verification parameters - UserId: {UserId}, Token: {HasToken}", 
                        userId ?? "null", !string.IsNullOrEmpty(token));
                    return BadRequest(new { message = "Invalid verification link." });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found for verification - UserId: {UserId}", userId);
                    return BadRequest(new { message = "Invalid verification link." });
                }

                if (user.EmailConfirmed)
                {
                    _logger.LogInformation("Email already verified for user: {Email}", user.Email);
                    return Ok(new { message = "Email is already verified.", success = true });
                }

                // URL decode the token (in case it wasn't decoded by the framework)
                var decodedToken = Uri.UnescapeDataString(token);
                
                var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
                if (!result.Succeeded)
                {
                    _logger.LogWarning("Email verification failed for user: {Email}. Errors: {Errors}", 
                        user.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                    return BadRequest(new { 
                        message = "Email verification failed. The link may have expired or is invalid.", 
                        errors = result.Errors.Select(e => e.Description) 
                    });
                }

                _logger.LogInformation("Email verified successfully for user: {Email}", user.Email);
                return Ok(new { message = "Email verified successfully! You can now log in.", success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during email verification for UserId: {UserId}", userId ?? "null");
                return StatusCode(500, new { 
                    message = "An error occurred during email verification.",
                    details = ex.Message 
                });
            }
        }

        // GET: api/auth/users - Get all users (Admin only)
        [HttpGet("users")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<object>>> GetUsers(
            [FromQuery] string? role = null,
            [FromQuery] string? search = null)
        {
            try
            {
                var query = _userManager.Users.AsQueryable();

                // Filter by role if specified
                if (!string.IsNullOrEmpty(role))
                {
                    query = query.Where(u => u.Role == role);
                }

                // Search by name or email if specified
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(u => 
                        u.FirstName.Contains(search) || 
                        u.LastName.Contains(search) || 
                        u.Email!.Contains(search));
                }

                var users = await query
                    .Select(u => new
                    {
                        id = u.Id,
                        firstName = u.FirstName,
                        lastName = u.LastName,
                        email = u.Email,
                        username = u.UserName,
                        role = u.Role,
                        isActive = !u.IsBlocked,
                        lastLoginAt = u.LastLoginDate,
                        createdAt = u.CreatedAt,
                        updatedAt = u.UpdatedAt,
                        emailConfirmed = u.EmailConfirmed
                    })
                    .OrderByDescending(u => u.createdAt)
                    .AsNoTracking()
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return StatusCode(500, new { message = "An error occurred while retrieving users." });
            }
        }

        // GET: api/auth/users/{id} - Get specific user (Admin only)
        [HttpGet("users/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUser(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found." });
                }

                var userInfo = new
                {
                    id = user.Id,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    email = user.Email,
                    username = user.UserName,
                    role = user.Role,
                    isActive = !user.IsBlocked,
                    lastLoginAt = user.LastLoginDate,
                    createdAt = user.CreatedAt,
                    updatedAt = user.UpdatedAt,
                    emailConfirmed = user.EmailConfirmed
                };

                return Ok(userInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user");
                return StatusCode(500, new { message = "An error occurred while retrieving user." });
            }
        }

        // PUT: api/auth/users/{id} - Update user (Admin only)
        [HttpPut("users/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserDto updateDto)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found." });
                }

                // Update user properties
                user.FirstName = updateDto.FirstName;
                user.LastName = updateDto.LastName;
                user.Email = updateDto.Email;
                user.UserName = updateDto.Email;
                user.Role = updateDto.Role ?? user.Role;
                user.IsBlocked = !(updateDto.IsActive ?? true);
                user.UpdatedAt = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    return BadRequest(new { message = "Failed to update user.", errors = result.Errors });
                }

                // Update password if provided
                if (!string.IsNullOrEmpty(updateDto.Password))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var passwordResult = await _userManager.ResetPasswordAsync(user, token, updateDto.Password);
                    if (!passwordResult.Succeeded)
                    {
                        return BadRequest(new { message = "Failed to update password.", errors = passwordResult.Errors });
                    }
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating user {id}");
                return StatusCode(500, new { message = "An error occurred while updating user." });
            }
        }

        // POST: api/auth/test-email - Test email functionality (Admin only)
        [HttpPost("test-email")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TestEmail([FromBody] EmailDto testEmailDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var success = await _emailService.SendEmailAsync(
                    testEmailDto.Email, 
                    "Test Email from Portfolio", 
                    "<h2>Test Email</h2><p>If you receive this, your email service is working correctly!</p>");

                if (success)
                {
                    return Ok(new { message = "Test email sent successfully!" });
                }
                else
                {
                    return BadRequest(new { message = "Failed to send test email." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending test email");
                return StatusCode(500, new { message = "An error occurred while sending test email." });
            }
        }

        // DELETE: api/auth/users/{id} - Delete user (Admin only)
        [HttpDelete("users/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found." });
                }

                // Don't allow deleting the last admin
                if (user.Role == "Admin")
                {
                    var adminCount = await _userManager.Users
                        .CountAsync(u => u.Role == "Admin");
                    if (adminCount <= 1)
                    {
                        return BadRequest(new { message = "Cannot delete the last admin user." });
                    }
                }

                _logger.LogInformation("Starting deletion of related data for user: {UserId}", id);

                // Delete UserPurchases first (they have FK to Payments)
                var purchases = await _context.UserPurchases
                    .Where(up => up.UserId == id)
                    .ToListAsync();
                if (purchases.Any())
                {
                    _context.UserPurchases.RemoveRange(purchases);
                    _logger.LogInformation("Deleting {Count} user purchases", purchases.Count);
                }

                // Delete Payments
                var payments = await _context.Payments
                    .Where(p => p.UserId == id)
                    .ToListAsync();
                if (payments.Any())
                {
                    _context.Payments.RemoveRange(payments);
                    _logger.LogInformation("Deleting {Count} payments", payments.Count);
                }

                // Delete or nullify CommentLikes
                var commentLikes = await _context.CommentLikes
                    .Where(cl => cl.UserId == id)
                    .ToListAsync();
                if (commentLikes.Any())
                {
                    // Try to set to null first, if that fails, delete them
                    foreach (var like in commentLikes)
                    {
                        like.UserId = null;
                    }
                    _logger.LogInformation("Nullifying {Count} comment likes", commentLikes.Count);
                }

                // Delete or nullify PublicationComments
                var comments = await _context.PublicationComments
                    .Where(pc => pc.UserId == id)
                    .ToListAsync();
                if (comments.Any())
                {
                    foreach (var comment in comments)
                    {
                        comment.UserId = null;
                    }
                    _logger.LogInformation("Nullifying {Count} comments", comments.Count);
                }

                // Delete or nullify PublicationRatings
                var ratings = await _context.PublicationRatings
                    .Where(pr => pr.UserId == id)
                    .ToListAsync();
                if (ratings.Any())
                {
                    foreach (var rating in ratings)
                    {
                        rating.UserId = null;
                    }
                    _logger.LogInformation("Nullifying {Count} ratings", ratings.Count);
                }

                // Nullify ContactForms UserId
                var contactForms = await _context.ContactForms
                    .Where(cf => cf.UserId == id)
                    .ToListAsync();
                if (contactForms.Any())
                {
                    foreach (var cf in contactForms)
                    {
                        cf.UserId = null;
                    }
                    _logger.LogInformation("Nullifying {Count} contact forms", contactForms.Count);
                }

                try
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Successfully saved changes for related data");
                }
                catch (DbUpdateException dbEx)
                {
                    _logger.LogError(dbEx, "Database error while deleting related data. Inner exception: {Inner}", dbEx.InnerException?.Message);
                    await transaction.RollbackAsync();
                    return StatusCode(500, new { 
                        message = "Database constraint error. The migration may not have been applied yet.", 
                        details = dbEx.InnerException?.Message ?? dbEx.Message 
                    });
                }

                // Now delete the user
                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(new { message = "Failed to delete user.", errors = result.Errors });
                }

                await transaction.CommitAsync();

                _logger.LogInformation("User deleted successfully: {UserId}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error deleting user {UserId}", id);
                return StatusCode(500, new { message = "An error occurred while deleting user.", details = ex.Message });
            }
        }
    }
}
