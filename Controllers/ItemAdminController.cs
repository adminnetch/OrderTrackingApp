using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderTrackingApp.Models;
using OrderTrackingApp.Filters;

namespace OrderTrackingApp.Controllers
{
    [Route("rental/admin/item")]
    [HasPermission("Rental.Admin")]
    public class ItemAdminController : Controller
    {
        private readonly AppDbContext _context;

        public ItemAdminController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("")]
        [HttpGet("index")]
        public async Task<IActionResult> Index()
        {
            var items = await _context.RentalItems
                .Include(i => i.Category)
                .ToListAsync();

            return View("~/Views/Rental/Admin/Item/Index.cshtml", items);
        }

        [HttpGet("create")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View("~/Views/Rental/Admin/Item/Create.cshtml");
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RentalItem item)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _context.Categories.ToListAsync();
                return View("~/Views/Rental/Admin/Item/Create.cshtml", item);
            }

            _context.RentalItems.Add(item);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpGet("edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _context.RentalItems.FindAsync(id);
            if (item == null) return NotFound();

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View("~/Views/Rental/Admin/Item/Edit.cshtml", item);
        }

        [HttpPost("edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RentalItem updated)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _context.Categories.ToListAsync();
                return View("~/Views/Rental/Admin/Item/Edit.cshtml", updated);
            }

            var item = await _context.RentalItems.FindAsync(id);
            if (item == null) return NotFound();

            item.Name = updated.Name;
            item.CategoryId = updated.CategoryId;
            item.PhotoPath = updated.PhotoPath;
            item.IsAvailable = updated.IsAvailable;

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpGet("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.RentalItems
                .Include(i => i.Category)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (item == null) return NotFound();

            return View("~/Views/Rental/Admin/Item/Delete.cshtml", item);
        }

        [HttpPost("delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmDelete(int id)
        {
            var item = await _context.RentalItems.FindAsync(id);
            if (item == null) return NotFound();

            _context.RentalItems.Remove(item);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}
