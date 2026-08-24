using System.ComponentModel.DataAnnotations;

namespace OrderTrackingApp.Models
{
    public class RentalItem
    {
        public int Id { get; set; }

        [Required, StringLength(150)]
        public string Name { get; set; } = string.Empty;

        // FK verso Category
        [Display(Name = "Categoria")]
        public int CategoryId { get; set; }

        // Nav property resa nullable
        public Category? Category { get; set; }

        public string? PhotoPath { get; set; }

        public bool IsAvailable { get; set; } = true;
    }
}
