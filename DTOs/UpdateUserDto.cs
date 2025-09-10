using System.ComponentModel.DataAnnotations;

namespace CodeNex.DTOs
{
    public class UpdateUserDto : IValidatableObject
    {
        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        // Optional password fields for updates
        [StringLength(100, MinimumLength = 8)]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        public string? ConfirmPassword { get; set; }

        [StringLength(50)]
        public string? Role { get; set; }
        
        public bool? IsActive { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Only validate password confirmation if password is provided
            if (!string.IsNullOrEmpty(Password))
            {
                if (string.IsNullOrEmpty(ConfirmPassword))
                {
                    yield return new ValidationResult(
                        "Confirmation password is required when password is provided.", 
                        new[] { nameof(ConfirmPassword) });
                }
                else if (Password != ConfirmPassword)
                {
                    yield return new ValidationResult(
                        "Password and confirmation password do not match.", 
                        new[] { nameof(ConfirmPassword) });
                }
            }
        }
    }
}
