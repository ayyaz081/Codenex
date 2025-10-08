using System.ComponentModel.DataAnnotations;

namespace CodeNex.Models
{
    public class UserPurchase
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public required string UserId { get; set; }

        [Required]
        public int RepositoryId { get; set; }

        [Required]
        public int PaymentId { get; set; }

        [Required]
        [StringLength(100)]
        public required string GitHubUsername { get; set; }

        public bool GitHubInviteSent { get; set; } = false;

        public DateTime? GitHubInviteSentAt { get; set; }

        public bool GitHubAccessGranted { get; set; } = false;

        public DateTime? GitHubAccessGrantedAt { get; set; }

        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public User? User { get; set; }
        public Repository? Repository { get; set; }
        public Payment? Payment { get; set; }
    }
}
