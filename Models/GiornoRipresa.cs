using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderTrackingApp.Models
{
    public class GiornoRipresa
    {
        public int Id { get; set; }

        [Required]
        public int NumeroGiorno { get; set; } // Giorno 1, 2, 3, ecc.

        public string? Osservazioni { get; set; }

        // RELAZIONI
        public List<ScenaRipresa> Scene { get; set; } = new();
        public List<AttoreRipresa> Attori { get; set; } = new();
        public List<LocationRipresa> Locations { get; set; } = new();

        [ForeignKey("PianoDiLavorazione")]
        public int PianoDiLavorazioneId { get; set; }
        public PianoDiLavorazione PianoDiLavorazione { get; set; } = null!;
    }
}
