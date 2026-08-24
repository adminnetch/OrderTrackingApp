using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OrderTrackingApp.Models;

namespace OrderTrackingApp.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly UserManager<User> _userMgr;
        private readonly AppDbContext      _db;

        public PermissionService(UserManager<User> userMgr, AppDbContext db)
        {
            _userMgr = userMgr;
            _db      = db;
        }

        public async Task<bool> HasPermissionAsync(ClaimsPrincipal user,
                                                   string permissionName,
                                                   int? entityId = null)
        {
            // 1) Utente autenticato?
            var u = await _userMgr.GetUserAsync(user);
            if (u == null) return false;

            // 2) SUPERADMIN BYPASS: se ha il ruolo SuperAdmin, ha sempre accesso
            var isSuperAdmin = await _userMgr.IsInRoleAsync(u, "SuperAdmin");
            if (isSuperAdmin) return true;

            int? projectIdToCheck = entityId;

            // 2) Se è permesso su RentalRequest, estrai CinemaOrderId
            if (entityId.HasValue 
                && permissionName.StartsWith("RentalRequest.", StringComparison.OrdinalIgnoreCase))
            {
                projectIdToCheck = await _db.RentalRequests
                    .Where(r => r.Id == entityId.Value)
                    .Select(r => (int?)r.CinemaOrderId)
                    .FirstOrDefaultAsync();
            }

            // 3) Permesso a livello di progetto?
            if (projectIdToCheck.HasValue)
            {
                var hasOnProject = await _db.ProjectPermissions
                    .AnyAsync(pp =>
                        pp.UserId         == u.Id &&
                        pp.ProjectId      == projectIdToCheck.Value &&
                        pp.PermissionName == permissionName
                    );
                if (hasOnProject) return true;
            }

            // 4) Permesso globale?
            var hasGlobal = await _db.PermessiUtente
                .Where(up => up.UserId == u.Id)
                .Select(up => up.Permission.Name)
                .ContainsAsync(permissionName);
            return hasGlobal;
        }
    }
}
