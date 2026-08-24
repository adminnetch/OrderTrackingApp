using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderTrackingApp.Models
{
    public class Trasporti
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string Auto { get; set; } = string.Empty;

        [Required]
        public string Chi { get; set; } = string.Empty;

        [Required]
        public string Dove { get; set; } = string.Empty;

        [Required]
        public string Ora { get; set; } = string.Empty;

        // 🔹 Chiave esterna per ODGOrder
        [ForeignKey(nameof(ODGOrder))]
        public int? ODGOrderId { get; set; }

        public ODGOrder? ODGOrder { get; set; }
    }
}
