using System.ComponentModel.DataAnnotations;

namespace OrderTrackingApp.Models
{
    public class Permission
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string AppName { get; set; } = string.Empty; // Es: "ODG", "Finanze", ecc.

        public string? Description { get; set; }

        public ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
    }
}
