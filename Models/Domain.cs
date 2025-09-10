using System.ComponentModel.DataAnnotations;

namespace CodeNex.Models
{
    public class Domain
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;
        public string IconCssClass { get; set; } = string.Empty;
    }
}
