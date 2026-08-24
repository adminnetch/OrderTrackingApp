using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OrderTrackingApp.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        // Oggetti appartenenti a questa categoria
        public List<RentalItem> Items { get; set; } = new();
    }
}
