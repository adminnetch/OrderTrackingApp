using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderTrackingApp.Models
{
    public class Contatto
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string Nome { get; set; } = string.Empty;

        [Required]
        public string Ruolo { get; set; } = string.Empty;

        [Required]
        public string Email { get; set; } = string.Empty;

        // 🔹 Chiave esterna per ODGOrder
        [ForeignKey(nameof(ODGOrder))]
        public int? ODGOrderId { get; set; }

        public ODGOrder? ODGOrder { get; set; }
    }
}
