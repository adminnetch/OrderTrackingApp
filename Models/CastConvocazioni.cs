using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderTrackingApp.Models
{
    public class CastConvocazioni
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        // 🔹 Chiave esterna per ODGOrder
        [ForeignKey(nameof(ODGOrder))]
        public int? ODGOrderId { get; set; }

        public ODGOrder? ODGOrder { get; set; }  // Navigazione

        [Required]
        public string Attore { get; set; } = string.Empty;

        public string Costume { get; set; } = string.Empty;
        public string Trucco { get; set; } = string.Empty;
        public string PickUp { get; set; } = string.Empty;
        public string Pronti { get; set; } = string.Empty;
    }
}
