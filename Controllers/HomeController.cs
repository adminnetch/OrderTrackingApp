using Microsoft.AspNetCore.Mvc;
using OrderTrackingApp.Filters;
using OrderTrackingApp.Models;
using Microsoft.AspNetCore.Identity;

namespace OrderTrackingApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly AppDbContext _context;

        public HomeController(UserManager<User> userManager, AppDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                var userPermissions = _context.PermessiUtente
                    .Where(p => p.UserId == user.Id)
                    .Select(p => p.Permission.Name)
                    .ToList();

                if (userPermissions.Contains("Home.Index.Admin"))
                {
                    return View("IndexLoggedInAdmin");
                }
                else if (userPermissions.Contains("Home.Index.User"))
                {
                    return View("IndexLoggedIn");
                }
                else if (userPermissions.Contains("Home.Index.External"))
                {
                    return View("IndexLoggedInExternal");
                }
                else if (userPermissions.Contains("Home.Index.Manager"))
                {
                    return View("IndexLoggedInManager");
                }
                else if (userPermissions.Contains("Home.Index.Projects"))
                {
                    return View("IndexLoggedInProject");
                }
                else if (userPermissions.Contains("Home.Index.Orders"))
                {
                    return View("IndexLoggedInOrder");
                }
            }

            return View("IndexNotLoggedIn");
        }

        [HasPermission("Home.Privacy")]
        public IActionResult Privacy()
        {
            return View();
        }
    }
}
