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
                                  configuration["Stripe:WebhookSecret"] ?? string.Empty;

            _successUrl = Environment.GetEnvironmentVariable("STRIPE_SUCCESS_URL") ??
                         configuration["Stripe:SuccessUrl"] ??
                         "https://codenex.live/Repository.html?payment=success";

            _cancelUrl = Environment.GetEnvironmentVariable("STRIPE_CANCEL_URL") ??
                        configuration["Stripe:CancelUrl"] ??
                        "https://codenex.live/Repository.html?payment=cancel";

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

            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    _stripeWebhookSecret,
                    throwOnApiVersionMismatch: false
                );

                _logger.LogInformation($"Stripe webhook received: {stripeEvent.Type}");

                // Handle the checkout.session.completed event
                if (stripeEvent.Type == "checkout.session.completed")
                {
                    var session = stripeEvent.Data.Object as Session;
                    if (session == null)
                    {
                        _logger.LogWarning("Checkout session is null in webhook");
                        return BadRequest();
                    }

                    await HandleCheckoutSessionCompleted(session);
                }

                return Ok();
            }
            catch (StripeException stripeEx)
            {
                _logger.LogError(stripeEx, "Stripe webhook error");
                return BadRequest($"Stripe error: {stripeEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook");
                return StatusCode(500);
            }
        }

        private async Task HandleCheckoutSessionCompleted(Session session)
        {
            try
            {
                _logger.LogInformation($"Processing completed checkout session: {session.Id}");

                // Get metadata from session
                if (!session.Metadata.TryGetValue("userId", out var userId) ||
                    !session.Metadata.TryGetValue("repositoryId", out var repoIdStr) ||
                    !session.Metadata.TryGetValue("githubUsername", out var githubUsername))
                {
                    _logger.LogError("Missing metadata in checkout session");
                    return;
                }

                if (!int.TryParse(repoIdStr, out var repositoryId))
                {
                    _logger.LogError($"Invalid repository ID in metadata: {repoIdStr}");
                    return;
                }

                // Get repository
                var repository = await _context.Repositories.FindAsync(repositoryId);
                if (repository == null)
                {
                    _logger.LogError($"Repository not found: {repositoryId}");
                    return;
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
                    // Extract repo name from full name (e.g., "CodeNex-Premium/repo-name" -> "repo-name")
                    var repoName = repository.GitHubRepoFullName.Split('/').LastOrDefault() ?? repository.GitHubRepoFullName;

                    var inviteSuccess = await _githubService.InviteUserToRepositoryAsync(githubUsername, repoName);

                    if (inviteSuccess)
                    {
                        userPurchase.GitHubInviteSent = true;
                        userPurchase.GitHubInviteSentAt = DateTime.UtcNow;
                        userPurchase.GitHubAccessGranted = true;
                        userPurchase.GitHubAccessGrantedAt = DateTime.UtcNow;

                        await _context.SaveChangesAsync();

                        _logger.LogInformation($"GitHub access granted to {githubUsername} for repository {repoName}");
                    }
                    else
                    {
                        _logger.LogError($"Failed to grant GitHub access to {githubUsername} for repository {repoName}");
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
    }
}
