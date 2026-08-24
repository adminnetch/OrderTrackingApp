using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderTrackingApp.Models
{
    public class CentroCosto
    {
        public int Id { get; set; }

        [Required]
        public string Nome { get; set; } = string.Empty;

        [ForeignKey("CinemaOrder")]
        public int CinemaOrderId { get; set; }
        public CinemaOrder CinemaOrder { get; set; } = null!;

        public List<VoceSpesa> Spese { get; set; } = new();
    }
}
