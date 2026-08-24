using System.ComponentModel.DataAnnotations;

namespace OrderTrackingApp.Models
{
    public enum RelationshipLevel
    {
        Famiglia,
        Amico,
        Collega,
        Altro,
    }

    public class EmergencyContact
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TroupeCastContactId { get; set; }

        // <<< nullable, senza [Required]
        public TroupeCastContact? TroupeCastContact { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public RelationshipLevel Relation { get; set; }

        [Required]
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;
    }
}
