using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderTrackingApp.Models
{
    public class LocationRipresa
    {
        public int Id { get; set; }

        [Required]
        public string NomeLocation { get; set; } = string.Empty; // Es: Bagno, Fiume, Soggiorno

        [Required]
        public string TipoLocation { get; set; } = "INT"; // INT, EXT, INT/EXT

        [ForeignKey("GiornoRipresa")]
        public int GiornoRipresaId { get; set; }
        public GiornoRipresa GiornoRipresa { get; set; } = null!;
    }
}
