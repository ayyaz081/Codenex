using System.ComponentModel.DataAnnotations;

namespace CodeNex.DTOs
{
    public class CreateRatingDto
    {
        [Required]
        public int PublicationId { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }
    }

    public class RatingResponseDto
    {
        public int Id { get; set; }
        public int PublicationId { get; set; }
        public int Rating { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class UpdateRatingDto
    {
        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }
    }
}
