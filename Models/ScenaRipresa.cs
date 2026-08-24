using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderTrackingApp.Models
{
    public class ScenaRipresa
    {
        public int Id { get; set; }

        [Required]
        public string NumeroScena { get; set; } = string.Empty; // S1, S2, S3...

        public string? Descrizione { get; set; }

        [ForeignKey("GiornoRipresa")]
        public int GiornoRipresaId { get; set; }
        public GiornoRipresa GiornoRipresa { get; set; } = null!;
    }
}
