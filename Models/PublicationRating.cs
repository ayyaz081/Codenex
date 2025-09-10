using System.ComponentModel.DataAnnotations;

namespace CodeNex.Models
{
    public class PublicationRating
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PublicationId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Publication? Publication { get; set; }
        public virtual User? User { get; set; }
    }
}
