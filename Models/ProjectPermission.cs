using System.ComponentModel.DataAnnotations;

namespace OrderTrackingApp.Models
{
    public class ProjectPermission
    {
        public int Id { get; set; }

        [Required]
        public string? UserId { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [Required]
        public string? PermissionName { get; set; }
    }
}
