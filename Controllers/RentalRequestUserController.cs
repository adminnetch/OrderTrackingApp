using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;

using OrderTrackingApp.Models;
using OrderTrackingApp.Filters;

namespace OrderTrackingApp.Controllers
{
    [Route("rental/user")]
    public class RentalRequestUserController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;


        public RentalRequestUserController(AppDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("")]
        [HttpGet("index")]
        [HasPermission("Rental.User.Index")]
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var visualName = currentUser?.VisualName ?? "Utente sconosciuto";

            var requests = await _context.RentalRequests
                .Where(r => r.UserVisualName == visualName)
                .Include(r => r.CinemaOrder)
                .Include(r => r.RequestItems)
                    .ThenInclude(ri => ri.RentalItem)
                .ToListAsync();

            return View("~/Views/Rental/User/Index.cshtml", requests);
        }



        [HttpGet("create")]
        [HasPermission("Rental.User.Create")]
        public async Task<IActionResult> Create()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var visualName = currentUser?.VisualName ?? "Utente sconosciuto";

            var model = new RentalRequest
            {
                UserVisualName = visualName
            };

            ViewBag.Categories = await _context.Categories
                .Include(c => c.Items.Where(i => i.IsAvailable))
                .ToListAsync();

            ViewBag.CinemaOrders = new SelectList(
                await _context.CinemaOrders.ToListAsync(),
                "Id", "Title" // Cambia "ReferenceName" con il campo giusto se diverso
            );

            return View("~/Views/Rental/User/Create.cshtml", model);
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        [HasPermission("Rental.User.Create")]
        public async Task<IActionResult> Create(RentalRequest request, List<int> selectedItemIds)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var visualName = currentUser?.VisualName ?? "Utente sconosciuto";

            request.UserVisualName = visualName;

            // Evita errore su campo readonly
            ModelState.Remove(nameof(request.UserVisualName));

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _context.Categories
                    .Include(c => c.Items.Where(i => i.IsAvailable))
                    .ToListAsync();

                ViewBag.CinemaOrders = new SelectList(
                    await _context.CinemaOrders.ToListAsync(),
                    "Id", "Title"
                );

                return View("~/Views/Rental/User/Create.cshtml", request);
            }

            // Debug (opzionale)
            Console.WriteLine(">>> MODELSTATE VALID: " + ModelState.IsValid);
            Console.WriteLine(">>> CHECKBOX COUNT: " + selectedItemIds?.Count);
            Console.WriteLine(">>> CLIENT: " + request.Client);
            Console.WriteLine(">>> TYPE: " + request.Type);
            Console.WriteLine(">>> DATE IN: " + request.CheckIn);
            Console.WriteLine(">>> DATE OUT: " + request.CheckOut);

            request.Status = RentalStatus.Pending;
            request.IsEditableByUser = true;

            request.RequestItems = selectedItemIds.Select(id => new RentalRequestItem
            {
                RentalItemId = id
            }).ToList();

            _context.RentalRequests.Add(request);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }




        [HttpGet("details/{id}")]
        [HasPermission("Rental.User.Details")]
        public async Task<IActionResult> Details(int id)
        {
            var request = await _context.RentalRequests
                .Include(r => r.CinemaOrder)
                .Include(r => r.RequestItems)
                    .ThenInclude(ri => ri.RentalItem)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
                return NotFound();

            return View("~/Views/Rental/User/Details.cshtml", request);
        }



        [HttpGet("edit/{id}")]
        [HasPermission("Rental.User.Edit")]
        public async Task<IActionResult> Edit(int id)
        {
            Console.WriteLine(">>> Edit GET invocato con ID: " + id);
            var request = await _context.RentalRequests
                .Include(r => r.RequestItems)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null || request.UserVisualName != User.Identity?.Name ||
                !(request.Status == RentalStatus.Pending || request.Status == RentalStatus.RejectedWithReason) ||
                !request.IsEditableByUser)
            {
                return NotFound();
            }

            ViewBag.Categories = await _context.Categories
                .Include(c => c.Items)
                .ToListAsync();

            ViewBag.CinemaOrders = new SelectList(
                await _context.CinemaOrders.ToListAsync(),
                "Id", "Title"
            );

            ViewBag.SelectedIds = request.RequestItems.Select(r => r.RentalItemId).ToList();

            return View("~/Views/Rental/User/Edit.cshtml", request);
        }

        [HttpPost("edit/{id}")]
        [ValidateAntiForgeryToken]
        [HasPermission("Rental.User.Edit")]
        public async Task<IActionResult> Edit(int id, RentalRequest updated, List<int> selectedItemIds)
        {
            var request = await _context.RentalRequests
                .Include(r => r.RequestItems)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null || request.UserVisualName != User.Identity?.Name ||
                !(request.Status == RentalStatus.Pending || request.Status == RentalStatus.RejectedWithReason) ||
                !request.IsEditableByUser)
            {
                return NotFound();
            }

            ModelState.Remove(nameof(updated.UserVisualName));

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _context.Categories
                    .Include(c => c.Items)
                    .ToListAsync();

                ViewBag.CinemaOrders = new SelectList(
                    await _context.CinemaOrders.ToListAsync(),
                    "Id", "Title"
                );

                ViewBag.SelectedIds = selectedItemIds;
                return View("~/Views/Rental/User/Edit.cshtml", updated);
            }

            request.CheckIn = updated.CheckIn;
            request.CheckOut = updated.CheckOut;
            request.Client = updated.Client;
            request.Type = updated.Type;
            request.CinemaOrderId = updated.CinemaOrderId;
            request.Status = RentalStatus.Pending;

            _context.RentalRequestItems.RemoveRange(request.RequestItems);
            request.RequestItems = selectedItemIds.Select(id => new RentalRequestItem
            {
                RentalItemId = id,
                RentalRequestId = request.Id
            }).ToList();

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }



        [HttpPost("delete/{id}")]
        [ValidateAntiForgeryToken]
        [HasPermission("Rental.User.Delete")]
        public async Task<IActionResult> Delete(int id)
        {
            var request = await _context.RentalRequests.FindAsync(id);
            if (request == null || request.UserVisualName != User.Identity?.Name || request.Status != RentalStatus.Pending)
                return NotFound();

            _context.RentalRequests.Remove(request);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }


        [HttpPost("report-damage")]
        [ValidateAntiForgeryToken]
        [HasPermission("Rental.User.ReportDamage")]
        public async Task<IActionResult> ReportDamage(int requestId, string description)
        {
            var request = await _context.RentalRequests.FindAsync(requestId);
            if (request == null || request.UserVisualName != User.Identity?.Name)
                return NotFound();

            if (request.Status != RentalStatus.Approved && request.Status != RentalStatus.MaterialDelivered)
                return BadRequest();

            var report = new DamageReport
            {
                RentalRequestId = requestId,
                Description = description,
                ReportedAt = DateTime.Now
            };

            _context.DamageReport.Add(report);
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { id = requestId });
        }


        [HttpGet("export-pdf/{id}")]
        [HasPermission("Rental.User.ExportPdf")]
        public IActionResult ExportPdf(int id)
        {
            return Content("Funzione PDF non ancora implementata.");
        }
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Content("Il controller funziona");
        }
    }
}
