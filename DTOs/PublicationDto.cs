using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace CodeNex.DTOs
{
    public class PublicationDto
    {
        [StringLength(200)]
        public string? Title { get; set; }

        [StringLength(500)]
        public string? Authors { get; set; }

        [StringLength(100)]
        public string? Domain { get; set; }

        [StringLength(2000)]
        public string? Abstract { get; set; }

        [StringLength(500)]
        public string? Keywords { get; set; }

        public DateTime? PublishedDate { get; set; }

        public bool? IsPublished { get; set; }
    }

    public class PublicationUploadDto
    {
        [Required]
        [StringLength(200)]
        public required string Title { get; set; }

        [Required]
        [StringLength(500)]
        public required string Authors { get; set; }

        [Required]
        [StringLength(100)]
        public required string Domain { get; set; }

        [Required]
        [StringLength(2000)]
        public required string Abstract { get; set; }

        [StringLength(500)]
        public string? Keywords { get; set; }

        public DateTime? PublishedDate { get; set; }

        public IFormFile? ThumbnailFile { get; set; }

        public IFormFile? DocumentFile { get; set; }
    }

    public class PublicationCommentDto
    {
        [Required]
        [StringLength(1000)]
        public required string Comment { get; set; }
    }

    public class PublicationRatingDto
    {
        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }
    }
}
