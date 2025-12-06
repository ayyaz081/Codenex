using System.ComponentModel.DataAnnotations;

namespace Codenex.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required, StringLength(500)]
        public string ShortDescription { get; set; } = string.Empty;

        [Required]
        public string LongDescription { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string Domain { get; set; } = string.Empty;

        public string ImageUrl { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Foreign key (optional)
        public int? RepositoryId { get; set; }

        // Navigation properties
        public virtual Repository? Repository { get; set; }
        public virtual ICollection<Publication> Publications { get; set; } = new List<Publication>();
    }
}
