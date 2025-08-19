using System.ComponentModel.DataAnnotations;

namespace PortfolioBackend.DTOs
{
    public class ContactFormDto
    {
        [Required]
        [StringLength(100)]
        public required string Name { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public required string Email { get; set; }

        [Required]
        [StringLength(200)]
        public required string Subject { get; set; }

        [Required]
        [StringLength(2000)]
        public required string Message { get; set; }
    }

    public class UserRegistrationDto
    {
        [Required]
        [StringLength(100)]
        public required string FirstName { get; set; }

        [Required]
        [StringLength(100)]
        public required string LastName { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public required string Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public required string Password { get; set; }
    }

    public class UserLoginDto
    {
        [Required]
        [EmailAddress]
        [StringLength(255)]
        public required string Email { get; set; }

        [Required]
        [StringLength(100)]
        public required string Password { get; set; }
    }

    public class SearchDto
    {
        [Required]
        [StringLength(200)]
        public required string Query { get; set; }

        [StringLength(50)]
        public string? Type { get; set; } // Products, Solutions, Publications, Repository, All
    }
}
