using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using OrderTrackingApp.Models;
using OrderTrackingApp.Services;

namespace OrderTrackingApp.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class HasPermissionAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly string _requiredPermission;

        public HasPermissionAttribute(string permission)
        {
            _requiredPermission = permission;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var http    = context.HttpContext;
            var userMgr = http.RequestServices.GetRequiredService<UserManager<User>>();

            // 1) Utente non autenticato → redirect a login
            var userEntity = await userMgr.GetUserAsync(http.User);
            if (userEntity == null)
            {
                context.Result = new RedirectToActionResult(
                    "Login", "Account", new { area = "" });
                return;
            }

            // 2) Estrai eventuale {xxxId} da route, query o form
            int? entityId = null;
            foreach (var kv in context.RouteData.Values)
            {
                if (kv.Key.EndsWith("Id", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(kv.Value?.ToString(), out var pid))
                {
                    entityId = pid;
                    break;
                }
            }

            if (!entityId.HasValue)
            {
                foreach (var qs in http.Request.Query)
                {
                    if (qs.Key.EndsWith("Id", StringComparison.OrdinalIgnoreCase)
                        && int.TryParse(qs.Value, out var pid))
                    {
                        entityId = pid;
                        break;
                    }
                }
            }

            if (!entityId.HasValue && http.Request.HasFormContentType)
            {
                foreach (var fm in http.Request.Form)
                {
                    if (fm.Key.EndsWith("Id", StringComparison.OrdinalIgnoreCase)
                        && int.TryParse(fm.Value, out var pid))
                    {
                        entityId = pid;
                        break;
                    }
                }
            }

            // 3) Usa PermissionService per verificare permesso (globale o a progetto)
            var permSvc = http.RequestServices.GetRequiredService<IPermissionService>();
            var hasPerm = await permSvc.HasPermissionAsync(
                http.User,
                _requiredPermission,
                entityId
            );
            if (hasPerm)
            {
                return;
            }

            // 4) Autenticato ma senza permesso → redirect a AccessDenied
            context.Result = new RedirectToActionResult(
                "AccessDenied", "Account", new { area = "" });
        }
    }
}
