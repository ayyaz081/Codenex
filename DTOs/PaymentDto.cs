using System.ComponentModel.DataAnnotations;

namespace CodeNex.DTOs
{
    public class CreateCheckoutSessionDto
    {
        [Required]
        public int RepositoryId { get; set; }

        [Required]
        [StringLength(100)]
        public required string GitHubUsername { get; set; }
    }

    public class CheckoutSessionResponseDto
    {
        public required string SessionId { get; set; }
        public required string PublishableKey { get; set; }
    }

    public class VerifyPurchaseDto
    {
        public bool HasPurchased { get; set; }
        public bool GitHubAccessGranted { get; set; }
        public string? GitHubUsername { get; set; }
        public DateTime? PurchaseDate { get; set; }
    }

    public class UserPurchaseDto
    {
        public int Id { get; set; }
        public int RepositoryId { get; set; }
        public required string RepositoryTitle { get; set; }
        public decimal Amount { get; set; }
        public required string GitHubUsername { get; set; }
        public bool GitHubAccessGranted { get; set; }
        public DateTime PurchaseDate { get; set; }
    }
}
