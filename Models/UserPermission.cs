using System.ComponentModel.DataAnnotations.Schema;

namespace OrderTrackingApp.Models
{
    public class UserPermission
    {
        public int Id { get; set; }

        [ForeignKey("User")]
        public string UserId { get; set; } = string.Empty;
        public User User { get; set; } = null!;

        [ForeignKey("Permission")]
        public int PermissionId { get; set; }
        public Permission Permission { get; set; } = null!;
    }
}
