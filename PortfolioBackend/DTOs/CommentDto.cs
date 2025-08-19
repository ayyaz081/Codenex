using System.ComponentModel.DataAnnotations;

namespace PortfolioBackend.DTOs
{
    public class CreateCommentDto
    {
        [Required]
        public int PublicationId { get; set; }

        [Required]
        [StringLength(1000, MinimumLength = 1)]
        public string Content { get; set; } = string.Empty;

        [Range(0, 5)]
        public int Rating { get; set; } = 0; // 0 means no rating, 1-5 for actual ratings
    }

    public class CommentResponseDto
    {
        public int Id { get; set; }
        public int PublicationId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsApproved { get; set; }
        public int Rating { get; set; }
        public int LikesCount { get; set; }
        public bool IsLiked { get; set; }
    }
}
