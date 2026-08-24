using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderTrackingApp.Models;
using OrderTrackingApp.Filters;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace OrderTrackingApp.Controllers
{
    public class OrderController : Controller
    {
        private readonly AppDbContext _context;

        public OrderController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [HasPermission("Ordini.View")]
        public IActionResult Index(string search)
        {
            var orders = _context.Orders.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                orders = orders.Where(o =>
                    o.OrderNumber.Contains(search) ||
                    o.CustomerName.Contains(search) ||
                    o.TrackingNumber.Contains(search));
            }

            ViewBag.Orders = orders.ToList();
            return View();
        }

        [HttpGet]
        [HasPermission("Ordini.Create")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Ordini.Create")]
        public async Task<IActionResult> Create(OrderTrackingApp.Models.Order order)
        {
            if (ModelState.IsValid)
            {
                // Order numbers are now generated in the Order constructor using thread-safe RandomNumberGenerator
                order.CreatedAt = DateTime.Now;
                order.LastUpdated = DateTime.Now;

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return View(order);
        }

        [HttpGet]
        [HasPermission("Ordini.Edit")]
        public IActionResult Edit(int id)
        {
            var order = _context.Orders.FirstOrDefault(o => o.Id == id);
            if (order == null)
                return NotFound();

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Ordini.Edit")]
        public async Task<IActionResult> Edit(OrderTrackingApp.Models.Order order)
        {
            if (ModelState.IsValid)
            {
                var existingOrder = await _context.Orders.FindAsync(order.Id);
                if (existingOrder == null)
                    return NotFound();

                existingOrder.CustomerName = order.CustomerName;
                existingOrder.CustomerEmail = order.CustomerEmail;
                existingOrder.CustomerPhone = order.CustomerPhone;
                existingOrder.CustomerAddress = order.CustomerAddress;
                existingOrder.Description = order.Description;
                existingOrder.Status = order.Status;
                existingOrder.LastUpdated = DateTime.Now;

                _context.Update(existingOrder);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Ordini.Delete")]
        public async Task<IActionResult> Delete(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return NotFound();

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet]
        [HasPermission("Ordini.View")]
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (order == null)
                return NotFound();

            return View(order);
        }

        [Authorize]
        [HttpGet("api/orders/states")]
        public async Task<IActionResult> GetOrderStates()
        {
            var states = await _context.Orders
                .Select(o => o.Status)
                .Distinct()
                .ToListAsync();
            return Ok(states);
        }

        [Authorize]
        [HttpGet("api/orders")]
        public async Task<IActionResult> GetOrders(string search, string status, DateTime? startDate, DateTime? endDate)
        {
            var orders = _context.Orders.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                orders = orders.Where(o =>
                    o.OrderNumber.Contains(search) ||
                    o.CustomerName.Contains(search) ||
                    o.TrackingNumber.Contains(search) ||
                    o.Status.Contains(search));
            }

            if (!string.IsNullOrEmpty(status))
                orders = orders.Where(o => o.Status == status);

            if (startDate.HasValue)
                orders = orders.Where(o => o.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                orders = orders.Where(o => o.CreatedAt <= endDate.Value);

            var result = await orders.ToListAsync();
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet("/Order/Tracking")]
        public IActionResult Tracking()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Tracking(string trackingNumber)
        {
            if (string.IsNullOrEmpty(trackingNumber))
            {
                ModelState.AddModelError("", "Inserisci un numero di tracciamento valido.");
                return View();
            }

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.TrackingNumber == trackingNumber);
            if (order == null)
            {
                return View("OrderNotFound");
            }

            return View("Track", order);
        }
    }
}
