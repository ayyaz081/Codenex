using System.ComponentModel.DataAnnotations;

namespace CodeNex.Models
{
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public required string UserId { get; set; }

        [Required]
        public int RepositoryId { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(255)]
        public required string StripePaymentIntentId { get; set; }

        [Required]
        [StringLength(50)]
        public required string Status { get; set; } // Pending, Completed, Refunded, Failed

        [StringLength(255)]
        public string? StripeCustomerId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        // Navigation properties
        public User? User { get; set; }
        public Repository? Repository { get; set; }
    }
}
