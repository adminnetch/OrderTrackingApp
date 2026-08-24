using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderTrackingApp.Models;
using OrderTrackingApp.Filters;

namespace OrderTrackingApp.Controllers
{
    [Route("rental/admin")]
    [HasPermission("Rental.Admin")]
    public class RentalRequestAdminController : Controller
    {
        private readonly AppDbContext _context;

        public RentalRequestAdminController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("")]
        [HttpGet("index")]
        public async Task<IActionResult> Index()
        {
            var requests = await _context.RentalRequests
                .Include(r => r.RequestItems).ThenInclude(i => i.RentalItem)
                .ToListAsync();

            return View("~/Views/Rental/Admin/Index.cshtml", requests);
        }

        [HttpGet("details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var request = await _context.RentalRequests
                .Include(r => r.RequestItems).ThenInclude(i => i.RentalItem)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null) return NotFound();

            return View("~/Views/Rental/Admin/Details.cshtml", request);
        }

        [HttpPost("approve/{id}")]
        public async Task<IActionResult> Approve(int id)
        {
            var request = await _context.RentalRequests.FindAsync(id);
            if (request == null) return NotFound();

            request.Status = RentalStatus.Approved;
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { id });
        }

        [HttpPost("reject-with-reason/{id}")]
        public async Task<IActionResult> RejectWithReason(int id, string reason)
        {
            var request = await _context.RentalRequests.FindAsync(id);
            if (request == null) return NotFound();

            request.Status = RentalStatus.RejectedWithReason;
            request.RejectionReason = reason;
            request.IsEditableByUser = true;

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { id });
        }

        [HttpPost("reject-without-reason/{id}")]
        public async Task<IActionResult> RejectWithoutReason(int id)
        {
            var request = await _context.RentalRequests.FindAsync(id);
            if (request == null) return NotFound();

            request.Status = RentalStatus.RejectedWithoutReason;
            request.IsEditableByUser = false;

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { id });
        }

        [HttpPost("confirm-delivery/{id}")]
        public async Task<IActionResult> ConfirmDelivery(int id)
        {
            var request = await _context.RentalRequests.FindAsync(id);
            if (request == null) return NotFound();

            request.Status = RentalStatus.MaterialDelivered;
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { id });
        }

        [HttpPost("close/{id}")]
        public async Task<IActionResult> Close(int id)
        {
            var request = await _context.RentalRequests.FindAsync(id);
            if (request == null) return NotFound();

            request.Status = RentalStatus.Closed;
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { id });
        }

        [HttpPost("archive/{id}")]
        public async Task<IActionResult> Archive(int id)
        {
            var request = await _context.RentalRequests.FindAsync(id);
            if (request == null) return NotFound();

            request.Status = RentalStatus.Archived;
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpGet("damages/{requestId}")]
        public async Task<IActionResult> DamageReports(int requestId)
        {
            var reports = await _context.DamageReport
                .Where(d => d.RentalRequestId == requestId)
                .ToListAsync();

            ViewBag.RequestId = requestId;
            return View("~/Views/Rental/Admin/DamageReports.cshtml", reports);
        }
    }
}
