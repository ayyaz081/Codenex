using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Neelsol.Models
{
    [Index(nameof(DisplayOrder))]
    [Index(nameof(IsActive))]
    [Index(nameof(IsApproved))]
    public class ClientTestimonial
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string ClientName { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string ClientPosition { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Message { get; set; } = string.Empty;

        [Range(1, 5)]
        public int Rating { get; set; } = 5;

        [MaxLength(255)]
        public string? ClientPhotoUrl { get; set; }

        [MaxLength(255)]
        public string? CompanyLogoUrl { get; set; }

        public int DisplayOrder { get; set; } = 999;

        public bool IsActive { get; set; } = true;

        public bool IsApproved { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
