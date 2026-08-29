using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OrderTrackingApp.Models;
using OrderTrackingApp.Filters;
using OrderTrackingApp.Services;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace OrderTrackingApp.Controllers
{
    public class CinemaController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;

        public CinemaController(AppDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        [HasPermission("Progetti.View")]
        public async Task<IActionResult> Index(string statusFilter)
        {
            var cinemaOrders = _context.CinemaOrders.AsQueryable();

            if (!string.IsNullOrEmpty(statusFilter))
                cinemaOrders = cinemaOrders.Where(o => o.Status == statusFilter);

            var cinemaOrdersList = await cinemaOrders.ToListAsync();
            return View(cinemaOrdersList);
        }

        [HasPermission("Progetti.Dashboard")]
        public async Task<IActionResult> Dashboard(int id)
        {
            var cinemaOrder = await _context.CinemaOrders
                .Include(c => c.Locations)
                .Include(c => c.ODGs)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cinemaOrder == null)
                return NotFound();

            return View(cinemaOrder);
        }

        [HasPermission("Progetti.Create")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Progetti.Create")]
        public async Task<IActionResult> Create(CinemaOrder cinemaOrder)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);

                cinemaOrder.ProjectNumber = GenerateSecureProjectNumber();
                cinemaOrder.CreatedAt = DateTime.Now;
                cinemaOrder.LastUpdated = DateTime.Now;
                cinemaOrder.CreatedBy = user?.VisualName ?? "Sconosciuto";

                _context.CinemaOrders.Add(cinemaOrder);
                await _context.SaveChangesAsync(); // serve per ottenere cinemaOrder.Id

                // ✅ CREA LA STRUTTURA CARTELLE
                try
                {
                    var storageService = HttpContext.RequestServices.GetRequiredService<ProjectStorageService>();
                    var folderPath = storageService.CreateProjectFolder(cinemaOrder.Id, cinemaOrder.Title);

                    Console.WriteLine($"Cartella progetto creata: {folderPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Errore creazione struttura cartelle progetto: {ex.Message}");
                    TempData["Error"] = "Il progetto è stato creato ma non è stato possibile generare le cartelle.";
                }

                return RedirectToAction("Index");
            }

            return View(cinemaOrder);
        }


        [HttpGet]
        [HasPermission("Progetti.Edit")]
        public async Task<IActionResult> Edit(int id)
        {
            var cinemaOrder = await _context.CinemaOrders.FindAsync(id);
            if (cinemaOrder == null)
                return NotFound();

            return View(cinemaOrder);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Progetti.Edit")]
        public async Task<IActionResult> Edit(CinemaOrder cinemaOrder)
        {
            if (ModelState.IsValid)
            {
                var existingCinemaOrder = await _context.CinemaOrders.FindAsync(cinemaOrder.Id);
                if (existingCinemaOrder == null)
                    return NotFound();

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }
                var userRoles = await _userManager.GetRolesAsync(user);

                if (userRoles.Contains("Manager"))
                {
                    existingCinemaOrder.Status = cinemaOrder.Status;
                    existingCinemaOrder.Notes = cinemaOrder.Notes;
                }
                else
                {
                    existingCinemaOrder.Director = cinemaOrder.Director;
                    existingCinemaOrder.Producer = cinemaOrder.Producer;
                    existingCinemaOrder.AssProducer = cinemaOrder.AssProducer;
                    existingCinemaOrder.DoP = cinemaOrder.DoP;
                    existingCinemaOrder.Status = cinemaOrder.Status;
                    existingCinemaOrder.DriveLink = cinemaOrder.DriveLink;
                    existingCinemaOrder.Notes = cinemaOrder.Notes;
                }

                existingCinemaOrder.LastUpdated = DateTime.Now;
                existingCinemaOrder.UpdatedBy = user?.VisualName ?? "Sconosciuto";

                _context.Update(existingCinemaOrder);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return View(cinemaOrder);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Progetti.Delete")]
        public async Task<IActionResult> Delete(int id)
        {
            var cinemaOrder = await _context.CinemaOrders.FindAsync(id);
            if (cinemaOrder == null)
                return NotFound();

            _context.CinemaOrders.Remove(cinemaOrder);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HasPermission("Progetti.View")]
        public async Task<IActionResult> Details(int id)
        {
            var cinemaOrder = await _context.CinemaOrders
                .Include(o => o.Locations)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (cinemaOrder == null)
                return NotFound();

            return View(cinemaOrder);
        }

        // ✅ API - Stati unici
        [Authorize]
        [HttpGet("api/cinemaorders/states")]
        public async Task<IActionResult> GetCinemaOrdersStates()
        {
            var states = await _context.CinemaOrders
                .Select(o => o.Status)
                .Distinct()
                .ToListAsync();
            return Ok(states);
        }

        // ✅ API - Filtro avanzato
        [Authorize]
        [HttpGet("api/cinemaorders")]
        public async Task<IActionResult> GetCinemaOrders(string search, string status, DateTime? startDate, DateTime? endDate)
        {
            var cinemaOrders = _context.CinemaOrders.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                cinemaOrders = cinemaOrders.Where(o =>
                    o.ProjectNumber.Contains(search) ||
                    o.Director.Contains(search) ||
                    o.Producer.Contains(search) ||
                    o.Status.Contains(search));
            }

            if (!string.IsNullOrEmpty(status))
                cinemaOrders = cinemaOrders.Where(o => o.Status == status);

            if (startDate.HasValue)
                cinemaOrders = cinemaOrders.Where(o => o.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                cinemaOrders = cinemaOrders.Where(o => o.CreatedAt <= endDate.Value);

            var result = await cinemaOrders.ToListAsync();
            return Ok(result);
        }

        // Thread-safe project number generation using cryptographic RandomNumberGenerator
        private static string GenerateSecureProjectNumber()
        {
            var bytes = new byte[4];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            // Map to 100000000-999999999 range
            uint num = BitConverter.ToUInt32(bytes, 0);
            return (num % 900000000 + 100000000).ToString();
        }
    }
}
