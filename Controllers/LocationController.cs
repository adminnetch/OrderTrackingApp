using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OrderTrackingApp.Models;
using OrderTrackingApp.Filters;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OrderTrackingApp.Controllers
{
    public class LocationController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;

        public LocationController(AppDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ✅ INDEX
        [HttpGet]
        [HasPermission("Location.View")]
        public async Task<IActionResult> Index(int cinemaOrderId)
        {
            var locations = await _context.Locations
                .Where(l => l.CinemaOrderId == cinemaOrderId)
                .ToListAsync();

            ViewBag.CinemaOrderId = cinemaOrderId;
            return View(locations);
        }

        // ✅ DETTAGLI
        [HttpGet]
        [HasPermission("Location.Details")]
        public async Task<IActionResult> Details(int id)
        {
            var location = await _context.Locations
                .Include(l => l.CinemaOrder)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (location == null)
                return NotFound();

            return View(location);
        }

        // ✅ CREA
        [HttpGet]
        [HasPermission("Location.Create")]
        public async Task<IActionResult> Create(int cinemaOrderId)
        {
            var project = await _context.CinemaOrders.FindAsync(cinemaOrderId);
            if (project == null)
                return NotFound();

            ViewBag.CinemaOrderId = cinemaOrderId;
            return View(new Location { CinemaOrderId = cinemaOrderId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Location.Create")]
        public async Task<IActionResult> Create(Location location)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);

                _context.Locations.Add(location);
                await _context.SaveChangesAsync();

                location.CreatedBy = user?.VisualName ?? "Sconosciuto";
                await _context.SaveChangesAsync();

                return RedirectToAction("Dashboard", "Cinema", new { id = location.CinemaOrderId });
            }

            ViewBag.CinemaOrderId = location.CinemaOrderId;
            return View(location);
        }

        // ✅ MODIFICA
        [HttpGet]
        [HasPermission("Location.Edit")]
        public async Task<IActionResult> Edit(int id)
        {
            var location = await _context.Locations.FindAsync(id);
            if (location == null)
                return NotFound();

            return View(location);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Location.Edit")]
        public async Task<IActionResult> Edit(Location location)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                var existing = await _context.Locations.FindAsync(location.Id);
                if (existing == null)
                    return NotFound();

                existing.ContactFirstName = location.ContactFirstName;
                existing.ContactLastName = location.ContactLastName;
                existing.ContactPhone = location.ContactPhone;
                existing.ContactEmail = location.ContactEmail;
                existing.LocationName = location.LocationName;
                existing.LocationType = location.LocationType;
                existing.Address = location.Address;
                existing.Description = location.Description;
                existing.GoogleMapsLink = location.GoogleMapsLink;
                existing.CartellaFotoLocation = location.CartellaFotoLocation;
                existing.UpdatedBy = user?.VisualName ?? "Sconosciuto";

                await _context.SaveChangesAsync();

                return RedirectToAction("Dashboard", "Cinema", new { id = existing.CinemaOrderId });
            }

            return View(location);
        }

        // ✅ ELIMINA
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Location.Delete")]
        public async Task<IActionResult> Delete(int id)
        {
            var location = await _context.Locations.FindAsync(id);
            if (location == null)
                return NotFound();

            int cinemaOrderId = location.CinemaOrderId;

            _context.Locations.Remove(location);
            await _context.SaveChangesAsync();

            return RedirectToAction("Dashboard", "Cinema", new { id = cinemaOrderId });
        }
    }
}
