using System.Security.Claims;
using System.Threading.Tasks;

namespace OrderTrackingApp.Services
{
    public interface IPermissionService
    {
        /// <summary>
        /// Ritorna true se l'utente ha il permesso (globale o a progetto).
        /// </summary>
        Task<bool> HasPermissionAsync(ClaimsPrincipal user,
                                      string permissionName,
                                      int? entityId = null);
    }
}
