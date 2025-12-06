using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Codenex.DTOs
{
    public class ProductUploadDto
    {
        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required, StringLength(500)]
        public string ShortDescription { get; set; } = string.Empty;

        [Required]
        public string LongDescription { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string Domain { get; set; } = string.Empty;

        public int? RepositoryId { get; set; }

        [Required]
        public IFormFile Image { get; set; } = default!;
    }
}
