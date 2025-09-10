using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace CodeNex.Models
{
    public class User : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(50)]
        public string Role { get; set; } = "User"; // Admin, Manager, User

        public bool IsBlocked { get; set; } = false;

        public DateTime LastLoginDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<PublicationComment> Comments { get; set; } = new List<PublicationComment>();
        public virtual ICollection<PublicationRating> Ratings { get; set; } = new List<PublicationRating>();
        public virtual ICollection<ContactForm> ContactForms { get; set; } = new List<ContactForm>();
        public virtual ICollection<CommentLike> CommentLikes { get; set; } = new List<CommentLike>();
    }
}
