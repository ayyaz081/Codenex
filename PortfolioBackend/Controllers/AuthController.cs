using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortfolioBackend.DTOs;
using PortfolioBackend.Models;
using PortfolioBackend.Services;
using System.Security.Claims;

namespace PortfolioBackend.Controllers
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

        public AuthController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            TokenService tokenService,
            IEmailService emailService,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpPost("register")]
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
                
                // Create verification URL
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var verificationUrl = $"{baseUrl}/Auth.html?action=verify&userId={user.Id}&token={Uri.EscapeDataString(emailConfirmationToken)}";
                
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
                    ExpiresAt = DateTime.UtcNow.AddHours(24)
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
                    ExpiresAt = DateTime.UtcNow.AddHours(24)
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
                
                // Construct the reset URL (frontend page with token)
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var resetUrl = $"{baseUrl}/Auth.html?action=reset&userId={user.Id}&token={Uri.EscapeDataString(resetToken)}";
                
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
        public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminDto createAdminDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
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
                    ExpiresAt = DateTime.UtcNow.AddHours(24)
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
                
                // Create verification URL
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var verificationUrl = $"{baseUrl}/Auth.html?action=verify&userId={user.Id}&token={Uri.EscapeDataString(emailConfirmationToken)}";
                
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

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromQuery] string userId, [FromQuery] string token)
        {
            try
            {
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
                {
                    return BadRequest(new { message = "Invalid verification link." });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return BadRequest(new { message = "Invalid verification link." });
                }

                var result = await _userManager.ConfirmEmailAsync(user, token);
                if (!result.Succeeded)
                {
                    return BadRequest(new { message = "Email verification failed.", errors = result.Errors });
                }

                return Ok(new { message = "Email verified successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during email verification");
                return StatusCode(500, new { message = "An error occurred during email verification." });
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

                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    return BadRequest(new { message = "Failed to delete user.", errors = result.Errors });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting user {id}");
                return StatusCode(500, new { message = "An error occurred while deleting user." });
            }
        }
    }
}
