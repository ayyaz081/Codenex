using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using CodeNex.Data;
using CodeNex.DTOs;
using CodeNex.Services;
using System.Security.Claims;

namespace CodeNex.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IGitHubService _githubService;
        private readonly ILogger<PaymentController> _logger;
        private readonly string _stripeSecretKey;
        private readonly string _stripePublishableKey;
        private readonly string _stripeWebhookSecret;
        private readonly string _successUrl;
        private readonly string _cancelUrl;

        public PaymentController(
            AppDbContext context,
            IGitHubService githubService,
            ILogger<PaymentController> logger,
            IConfiguration configuration)
        {
            _context = context;
            _githubService = githubService;
            _logger = logger;

            // Get Stripe configuration from environment
            _stripeSecretKey = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY") ??
                              configuration["Stripe:SecretKey"] ??
                              throw new InvalidOperationException("Stripe Secret Key is not configured");

            _stripePublishableKey = Environment.GetEnvironmentVariable("STRIPE_PUBLISHABLE_KEY") ??
                                   configuration["Stripe:PublishableKey"] ??
                                   throw new InvalidOperationException("Stripe Publishable Key is not configured");

            _stripeWebhookSecret = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET") ??
                                  configuration["Stripe:WebhookSecret"] ??
                                  throw new InvalidOperationException("Stripe Webhook Secret is not configured. This is required for secure payment processing.");

            _successUrl = Environment.GetEnvironmentVariable("STRIPE_SUCCESS_URL") ??
                         configuration["Stripe:SuccessUrl"] ??
                         throw new InvalidOperationException("Stripe Success URL is not configured");

            _cancelUrl = Environment.GetEnvironmentVariable("STRIPE_CANCEL_URL") ??
                        configuration["Stripe:CancelUrl"] ??
                        throw new InvalidOperationException("Stripe Cancel URL is not configured");

            // Set Stripe API key
            StripeConfiguration.ApiKey = _stripeSecretKey;

            _logger.LogInformation("Payment Controller initialized with Stripe integration");
        }

        // POST: api/payment/create-checkout-session
        [HttpPost("create-checkout-session")]
        [Authorize]
        public async Task<ActionResult<CheckoutSessionResponseDto>> CreateCheckoutSession(
            [FromBody] CreateCheckoutSessionDto dto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User not authenticated");
                }

                _logger.LogInformation($"Creating checkout session for user {userId}, repository {dto.RepositoryId}");

                // Get repository
                var repository = await _context.Repositories
                    .FirstOrDefaultAsync(r => r.Id == dto.RepositoryId && r.IsActive);

                if (repository == null)
                {
                    return NotFound("Repository not found");
                }

                if (!repository.IsPremium || repository.Price == null || repository.Price <= 0)
                {
                    return BadRequest("Repository is not premium or price is not set");
                }

                // Check if user already purchased
                var existingPurchase = await _context.UserPurchases
                    .FirstOrDefaultAsync(up => up.UserId == userId && 
                                              up.RepositoryId == dto.RepositoryId && 
                                              up.IsActive);

                if (existingPurchase != null)
                {
                    return BadRequest("You have already purchased this repository");
                }

                // Verify GitHub username
                var isValidUsername = await _githubService.VerifyGitHubUsernameAsync(dto.GitHubUsername);
                if (!isValidUsername)
                {
                    return BadRequest($"GitHub username '{dto.GitHubUsername}' does not exist");
                }

                // Create Stripe Checkout Session
                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                Currency = "usd",
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = repository.Title,
                                    Description = $"Premium GitHub Repository Access - {repository.Description}",
                                },
                                UnitAmount = (long)(repository.Price.Value * 100), // Convert to cents
                            },
                            Quantity = 1,
                        },
                    },
                    Mode = "payment",
                    SuccessUrl = $"{_successUrl}&session_id={{CHECKOUT_SESSION_ID}}&repo_id={dto.RepositoryId}",
                    CancelUrl = $"{_cancelUrl}&repo_id={dto.RepositoryId}",
                    Metadata = new Dictionary<string, string>
                    {
                        { "userId", userId },
                        { "repositoryId", dto.RepositoryId.ToString() },
                        { "githubUsername", dto.GitHubUsername }
                    }
                };

                var service = new SessionService();
                var session = await service.CreateAsync(options);

                _logger.LogInformation($"Stripe checkout session created: {session.Id}");

                return Ok(new CheckoutSessionResponseDto
                {
                    SessionId = session.Id,
                    PublishableKey = _stripePublishableKey
                });
            }
            catch (StripeException stripeEx)
            {
                _logger.LogError(stripeEx, "Stripe error creating checkout session");
                return StatusCode(500, $"Stripe error: {stripeEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating checkout session");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/payment/webhook
        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> StripeWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            
            _logger.LogInformation("=== STRIPE WEBHOOK RECEIVED ===");
            _logger.LogInformation($"Webhook payload length: {json?.Length ?? 0} bytes");
            _logger.LogInformation($"Webhook secret configured: {!string.IsNullOrEmpty(_stripeWebhookSecret)}");

            try
            {
                Event stripeEvent;
                
                // Always verify webhook signature for security
                var signature = Request.Headers["Stripe-Signature"].ToString();
                
                if (string.IsNullOrEmpty(signature))
                {
                    _logger.LogError("❌ Stripe webhook signature missing");
                    return BadRequest("Webhook signature required");
                }
                
                _logger.LogInformation($"Verifying Stripe webhook signature...");
                
                stripeEvent = EventUtility.ConstructEvent(
                    json,
                    signature,
                    _stripeWebhookSecret,
                    throwOnApiVersionMismatch: false
                );

                _logger.LogInformation($"✅ Stripe webhook received: {stripeEvent.Type}");
                _logger.LogInformation($"Event ID: {stripeEvent.Id}");

                // Handle the checkout.session.completed event
                if (stripeEvent.Type == "checkout.session.completed")
                {
                    _logger.LogInformation("Processing checkout.session.completed event");
                    
                    var session = stripeEvent.Data.Object as Session;
                    if (session == null)
                    {
                        _logger.LogError("❌ Checkout session is null in webhook");
                        return BadRequest("Session data is null");
                    }

                    _logger.LogInformation($"Session ID: {session.Id}, Payment Status: {session.PaymentStatus}");
                    await HandleCheckoutSessionCompleted(session);
                    
                    _logger.LogInformation("✅ Webhook processing completed successfully");
                }
                else
                {
                    _logger.LogInformation($"Ignoring webhook event type: {stripeEvent.Type}");
                }

                return Ok();
            }
            catch (StripeException stripeEx)
            {
                _logger.LogError(stripeEx, $"❌ Stripe webhook error: {stripeEx.Message}");
                _logger.LogError($"Stripe Error Code: {stripeEx.StripeError?.Code}, Type: {stripeEx.StripeError?.Type}");
                return BadRequest($"Stripe error: {stripeEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Unexpected error processing webhook");
                _logger.LogError($"Error type: {ex.GetType().Name}, Message: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        private async Task HandleCheckoutSessionCompleted(Session session)
        {
            try
            {
                _logger.LogInformation($"=== PROCESSING CHECKOUT SESSION ===");
                _logger.LogInformation($"Session ID: {session.Id}");
                _logger.LogInformation($"Payment Status: {session.PaymentStatus}");
                _logger.LogInformation($"Payment Intent ID: {session.PaymentIntentId}");
                
                // Log all metadata
                _logger.LogInformation($"Metadata count: {session.Metadata?.Count ?? 0}");
                if (session.Metadata != null)
                {
                    foreach (var kvp in session.Metadata)
                    {
                        _logger.LogInformation($"  Metadata[{kvp.Key}] = {kvp.Value}");
                    }
                }

                // Get metadata from session
                if (session.Metadata == null ||
                    !session.Metadata.TryGetValue("userId", out var userId) ||
                    !session.Metadata.TryGetValue("repositoryId", out var repoIdStr) ||
                    !session.Metadata.TryGetValue("githubUsername", out var githubUsername))
                {
                    _logger.LogError("❌ Missing required metadata in checkout session");
                    _logger.LogError($"UserId present: {session.Metadata?.ContainsKey("userId") ?? false}");
                    _logger.LogError($"RepositoryId present: {session.Metadata?.ContainsKey("repositoryId") ?? false}");
                    _logger.LogError($"GitHubUsername present: {session.Metadata?.ContainsKey("githubUsername") ?? false}");
                    return;
                }
                
                _logger.LogInformation($"✅ Metadata extracted - UserId: {userId}, RepoId: {repoIdStr}, GitHub: {githubUsername}");

                if (!int.TryParse(repoIdStr, out var repositoryId))
                {
                    _logger.LogError($"❌ Invalid repository ID in metadata: {repoIdStr}");
                    return;
                }

                // Get repository
                _logger.LogInformation($"Fetching repository with ID: {repositoryId}");
                var repository = await _context.Repositories.FindAsync(repositoryId);
                if (repository == null)
                {
                    _logger.LogError($"❌ Repository not found with ID: {repositoryId}");
                    return;
                }
                
                _logger.LogInformation($"✅ Repository found: {repository.Title}");
                _logger.LogInformation($"  IsPremium: {repository.IsPremium}");
                _logger.LogInformation($"  Price: {repository.Price}");
                _logger.LogInformation($"  GitHubRepoFullName: {repository.GitHubRepoFullName ?? "(not set)"}");
                
                if (string.IsNullOrEmpty(repository.GitHubRepoFullName))
                {
                    _logger.LogError($"❌ Repository {repositoryId} does not have GitHubRepoFullName set. Cannot grant GitHub access.");
                    // Continue to create payment/purchase records even if GitHub access cannot be granted
                }

                // Create payment record
                var payment = new Models.Payment
                {
                    UserId = userId,
                    RepositoryId = repositoryId,
                    Amount = repository.Price ?? 0,
                    StripePaymentIntentId = session.PaymentIntentId ?? session.Id,
                    Status = "Completed",
                    StripeCustomerId = session.CustomerId,
                    CreatedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Payment record created: {payment.Id}");

                // Create user purchase record
                var userPurchase = new Models.UserPurchase
                {
                    UserId = userId,
                    RepositoryId = repositoryId,
                    PaymentId = payment.Id,
                    GitHubUsername = githubUsername,
                    PurchaseDate = DateTime.UtcNow,
                    IsActive = true
                };

                _context.UserPurchases.Add(userPurchase);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"User purchase record created: {userPurchase.Id}");

                // Grant GitHub access
                if (!string.IsNullOrEmpty(repository.GitHubRepoFullName))
                {
                    _logger.LogInformation($"Processing GitHub access for repository: {repository.GitHubRepoFullName}, user: {githubUsername}");
                    
                    // Extract organization and repo name from full name (e.g., "CodeNex-Premium/repo-name")
                    var parts = repository.GitHubRepoFullName.Split('/', 2);
                    if (parts.Length != 2)
                    {
                        _logger.LogError($"Invalid GitHubRepoFullName format: {repository.GitHubRepoFullName}. Expected format: 'org/repo'");
                        return;
                    }
                    
                    var organizationName = parts[0];
                    var repoName = parts[1];
                    
                    _logger.LogInformation($"Parsed GitHub repository - Org: {organizationName}, Repo: {repoName}");

                    var inviteSuccess = await _githubService.InviteUserToRepositoryAsync(
                        githubUsername, 
                        repoName,
                        organizationName  // Pass the organization name from the repository record
                    );

                    if (inviteSuccess)
                    {
                        userPurchase.GitHubInviteSent = true;
                        userPurchase.GitHubInviteSentAt = DateTime.UtcNow;
                        userPurchase.GitHubAccessGranted = true;
                        userPurchase.GitHubAccessGrantedAt = DateTime.UtcNow;

                        await _context.SaveChangesAsync();

                        _logger.LogInformation($"✅ GitHub access granted successfully to {githubUsername} for repository {organizationName}/{repoName}");
                    }
                    else
                    {
                        _logger.LogError($"❌ Failed to grant GitHub access to {githubUsername} for repository {organizationName}/{repoName}");
                        _logger.LogError($"Please check: 1) GitHub PAT has correct permissions, 2) Repository {organizationName}/{repoName} exists, 3) User {githubUsername} is valid");
                    }
                }
                else
                {
                    _logger.LogWarning($"Repository {repositoryId} does not have GitHubRepoFullName set");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling checkout session completed");
            }
        }

        // GET: api/payment/verify-purchase/{repositoryId}
        [HttpGet("verify-purchase/{repositoryId}")]
        [Authorize]
        public async Task<ActionResult<VerifyPurchaseDto>> VerifyPurchase(int repositoryId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var purchase = await _context.UserPurchases
                    .FirstOrDefaultAsync(up => up.UserId == userId && 
                                              up.RepositoryId == repositoryId && 
                                              up.IsActive);

                if (purchase == null)
                {
                    return Ok(new VerifyPurchaseDto
                    {
                        HasPurchased = false,
                        GitHubAccessGranted = false
                    });
                }

                return Ok(new VerifyPurchaseDto
                {
                    HasPurchased = true,
                    GitHubAccessGranted = purchase.GitHubAccessGranted,
                    GitHubUsername = purchase.GitHubUsername,
                    PurchaseDate = purchase.PurchaseDate
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error verifying purchase for repository {repositoryId}");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/payment/user-purchases
        [HttpGet("user-purchases")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<UserPurchaseDto>>> GetUserPurchases()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var purchases = await _context.UserPurchases
                    .Include(up => up.Repository)
                    .Include(up => up.Payment)
                    .Where(up => up.UserId == userId && up.IsActive)
                    .OrderByDescending(up => up.PurchaseDate)
                    .Select(up => new UserPurchaseDto
                    {
                        Id = up.Id,
                        RepositoryId = up.RepositoryId,
                        RepositoryTitle = up.Repository!.Title,
                        Amount = up.Payment!.Amount,
                        GitHubUsername = up.GitHubUsername,
                        GitHubAccessGranted = up.GitHubAccessGranted,
                        PurchaseDate = up.PurchaseDate
                    })
                    .ToListAsync();

                return Ok(purchases);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user purchases");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/payment/verify-github-username
        [HttpPost("verify-github-username")]
        [AllowAnonymous]
        public async Task<ActionResult<bool>> VerifyGitHubUsername([FromBody] string githubUsername)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(githubUsername))
                {
                    return BadRequest("GitHub username is required");
                }

                var isValid = await _githubService.VerifyGitHubUsernameAsync(githubUsername);
                return Ok(isValid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error verifying GitHub username: {githubUsername}");
                return StatusCode(500, "Internal server error");
            }
        }
        
        // POST: api/payment/manual-grant-access/{purchaseId}
        // Manual endpoint to grant GitHub access for testing/troubleshooting
        [HttpPost("manual-grant-access/{purchaseId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ManualGrantAccess(int purchaseId)
        {
            try
            {
                _logger.LogInformation($"=== MANUAL GRANT ACCESS REQUEST ===");
                _logger.LogInformation($"Purchase ID: {purchaseId}");
                
                // Get the purchase record
                var purchase = await _context.UserPurchases
                    .Include(up => up.Repository)
                    .FirstOrDefaultAsync(up => up.Id == purchaseId);
                    
                if (purchase == null)
                {
                    _logger.LogError($"❌ Purchase not found: {purchaseId}");
                    return NotFound($"Purchase {purchaseId} not found");
                }
                
                _logger.LogInformation($"✅ Purchase found - User: {purchase.GitHubUsername}, Repo: {purchase.Repository?.GitHubRepoFullName}");
                
                if (purchase.Repository == null)
                {
                    _logger.LogError($"❌ Repository not found for purchase {purchaseId}");
                    return NotFound("Repository not found");
                }
                
                if (string.IsNullOrEmpty(purchase.Repository.GitHubRepoFullName))
                {
                    _logger.LogError($"❌ Repository does not have GitHubRepoFullName set");
                    return BadRequest("Repository GitHubRepoFullName is not set");
                }
                
                // Parse organization and repo name
                var parts = purchase.Repository.GitHubRepoFullName.Split('/', 2);
                if (parts.Length != 2)
                {
                    _logger.LogError($"❌ Invalid GitHubRepoFullName format: {purchase.Repository.GitHubRepoFullName}");
                    return BadRequest($"Invalid GitHubRepoFullName format: {purchase.Repository.GitHubRepoFullName}. Expected: org/repo");
                }
                
                var organizationName = parts[0];
                var repoName = parts[1];
                
                _logger.LogInformation($"Parsed - Org: {organizationName}, Repo: {repoName}");
                _logger.LogInformation($"Attempting to invite: {purchase.GitHubUsername}");
                
                // Grant GitHub access
                var inviteSuccess = await _githubService.InviteUserToRepositoryAsync(
                    purchase.GitHubUsername,
                    repoName,
                    organizationName
                );
                
                if (inviteSuccess)
                {
                    purchase.GitHubInviteSent = true;
                    purchase.GitHubInviteSentAt = DateTime.UtcNow;
                    purchase.GitHubAccessGranted = true;
                    purchase.GitHubAccessGrantedAt = DateTime.UtcNow;
                    
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation($"✅ Successfully granted access to {purchase.GitHubUsername}");
                    return Ok(new { 
                        success = true, 
                        message = $"Successfully invited {purchase.GitHubUsername} to {organizationName}/{repoName}",
                        githubUsername = purchase.GitHubUsername,
                        repository = $"{organizationName}/{repoName}"
                    });
                }
                else
                {
                    _logger.LogError($"❌ Failed to grant access to {purchase.GitHubUsername}");
                    return StatusCode(500, new { 
                        success = false, 
                        message = $"Failed to invite {purchase.GitHubUsername}. Check logs for details.",
                        githubUsername = purchase.GitHubUsername,
                        repository = $"{organizationName}/{repoName}"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in manual grant access for purchase {purchaseId}");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
    }
}
