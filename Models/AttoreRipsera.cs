using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderTrackingApp.Models
{
    public class AttoreRipresa
    {
        public int Id { get; set; }

        [Required]
        public string NomeAttore { get; set; } = string.Empty; // Es: Matilde, Helena Trovato

        [ForeignKey("GiornoRipresa")]
        public int GiornoRipresaId { get; set; }
        public GiornoRipresa GiornoRipresa { get; set; } = null!;
    }
}
