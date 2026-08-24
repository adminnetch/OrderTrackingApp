using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderTrackingApp.Models
{
    public class Location
    {
        public int Id { get; set; }

        [Required]
        public string ContactFirstName { get; set; } = string.Empty;

        [Required]
        public string ContactLastName { get; set; } = string.Empty;

        public string ContactPhone { get; set; } = string.Empty;

        public string ContactEmail { get; set; } = string.Empty;

        public string CreatedBy { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;

        [Required]
        public string LocationName { get; set; } = string.Empty;

        public string LocationType { get; set; } = string.Empty; // Interno, Esterno, ecc.

        public string Address { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string GoogleMapsLink { get; set; } = string.Empty;

        public string CartellaFotoLocation { get; set; } = string.Empty;

        // RELAZIONE CON CINEMAORDER
        [Required]
        public int CinemaOrderId { get; set; }

        [ForeignKey("CinemaOrderId")]
        public CinemaOrder? CinemaOrder { get; set; }

    }
}
