using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderTrackingApp.Models;
using OrderTrackingApp.Filters;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Linq;
using System.Threading.Tasks;

namespace OrderTrackingApp.Controllers
{
    public class TroupeCastContactsController : Controller
    {
        private readonly AppDbContext _ctx;
        public TroupeCastContactsController(AppDbContext ctx) => _ctx = ctx;

        // GET: /TroupeCastContacts?projectId=5   oppure  ?cinemaOrderId=5
        [HttpGet]
        [HasPermission("Contatti.View")]
        public async Task<IActionResult> Index(int? projectId, int? cinemaOrderId)
        {
            var id = projectId ?? cinemaOrderId
                     ?? throw new ArgumentException("projectId or cinemaOrderId is required");

            ViewBag.ProjectId = id;
            var list = await _ctx.TroupeCastContacts
                .Include(c => c.EmergencyContact)
                .Where(c => c.CinemaOrderId == id)
                .ToListAsync();
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> GetForProject(int projectId)
        {
            var contacts = await _ctx.TroupeCastContacts
                .Where(c => c.CinemaOrderId == projectId)
                .Select(c => new
                {
                    c.Id,
                    FullName = c.FirstName + " " + c.LastName,
                    Role = c.Role.ToString()
                })
                .ToListAsync();

            return Json(contacts);
        }


        // GET: Create
        [HttpGet]
        [HasPermission("Contatti.Create")]
        public IActionResult Create(int? projectId, int? cinemaOrderId)
        {
            var id = projectId ?? cinemaOrderId
                     ?? throw new ArgumentException("projectId or cinemaOrderId is required");

            ViewBag.ProjectId = id;
            PopulateDropdowns();

            var model = new TroupeCastContact
            {
                CinemaOrderId = id,
                EmergencyContact = new EmergencyContact()
            };
            return View(model);
        }

        // POST: Create
        [HttpPost]
        [HasPermission("Contatti.Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TroupeCastContact model, int? projectId, int? cinemaOrderId)
        {
            // Assicuriamoci di mantenere l'ID corretto, a prescindere da quale query string arrivi
            var id = projectId ?? cinemaOrderId ?? model.CinemaOrderId;
            model.CinemaOrderId = id;
            ViewBag.ProjectId = id;
            PopulateDropdowns();

            if (model.EmergencyContact == null)
                model.EmergencyContact = new EmergencyContact { TroupeCastContactId = model.Id };

            if (!ModelState.IsValid)
                return View(model);

            _ctx.Add(model);
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { projectId = id });
        }

        // GET: Edit
        [HttpGet]
        [HasPermission("Contatti.Edit")]
        public async Task<IActionResult> Edit(int id, int? projectId, int? cinemaOrderId)
        {
            // id è l'ID del contatto, ma dobbiamo anche pescare ProjectId
            var item = await _ctx.TroupeCastContacts
                .Include(c => c.EmergencyContact)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (item == null) return NotFound();

            var pid = projectId ?? cinemaOrderId ?? item.CinemaOrderId;
            ViewBag.ProjectId = pid;
            PopulateDropdowns();

            if (item.EmergencyContact == null)
                item.EmergencyContact = new EmergencyContact { TroupeCastContactId = item.Id };

            return View(item);
        }

        // POST: Edit
        [HttpPost]
        [HasPermission("Contatti.Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TroupeCastContact model, int? projectId, int? cinemaOrderId)
        {
            var id = projectId ?? cinemaOrderId ?? model.CinemaOrderId;
            model.CinemaOrderId = id;
            ViewBag.ProjectId = id;
            PopulateDropdowns();

            if (model.EmergencyContact == null)
                model.EmergencyContact = new EmergencyContact { TroupeCastContactId = model.Id };

            if (!ModelState.IsValid)
                return View(model);

            _ctx.Update(model);
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { projectId = id });
        }

        // GET: Details
        [HttpGet]
        [HasPermission("Contatti.Details")]
        public async Task<IActionResult> Details(int id)
        {
            var item = await _ctx.TroupeCastContacts
                .Include(c => c.EmergencyContact)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (item == null)
                return NotFound();

            return View(item);
        }

        // GET: Delete
        [HttpGet]
        [HasPermission("Contatti.Delete")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _ctx.TroupeCastContacts.FindAsync(id);
            if (item == null)
                return NotFound();

            return View(item);
        }

        // POST: DeleteConfirmed
        [HttpPost, ActionName("Delete")]
        [HasPermission("Contatti.Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _ctx.TroupeCastContacts.FindAsync(id)!;
            _ctx.TroupeCastContacts.Remove(item);
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { projectId = item.CinemaOrderId });
        }

        // GET: Export PDF
        [HttpGet]
        [HasPermission("Contatti.Export")]
        public async Task<IActionResult> ExportPdf(int projectId)
        {
            var contacts = await _ctx.TroupeCastContacts
                .Include(c => c.EmergencyContact)
                .Where(c => c.CinemaOrderId == projectId)
                .ToListAsync();

            byte[] pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(20);
                    page.Header().Text($"Contatti Progetto #{projectId}")
                                       .SemiBold().FontSize(20);

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.ConstantColumn(100);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("Nome");
                            header.Cell().Element(CellStyle).Text("Cognome");
                            header.Cell().Element(CellStyle).Text("Telefono");
                            header.Cell().Element(CellStyle).Text("Emergenza");
                        });

                        foreach (var c in contacts)
                        {
                            table.Cell().Element(CellStyle).Text(c.FirstName);
                            table.Cell().Element(CellStyle).Text(c.LastName);
                            table.Cell().Element(CellStyle).Text(c.PhoneNumber);
                            table.Cell().Element(CellStyle).Text(
                                c.EmergencyContact != null
                                ? $"{c.EmergencyContact.Name} ({c.EmergencyContact.PhoneNumber})"
                                : "-");
                        }

                        static IContainer CellStyle(IContainer container) =>
                            container.BorderBottom(1)
                                     .BorderColor(Colors.Grey.Lighten2)
                                     .PaddingVertical(5)
                                     .PaddingHorizontal(2);
                    });

                    page.Footer().AlignRight().Text(x =>
                    {
                        x.Span("Pagina ");
                        x.CurrentPageNumber();
                        x.Span(" di ");
                        x.TotalPages();
                    });
                });
            })
            .GeneratePdf();

            return File(pdf, "application/pdf", $"TroupeCastContacts_{projectId}.pdf");
        }

        private void PopulateDropdowns()
        {
            ViewBag.Roles         = Enum.GetValues<ProductionRole>();
            ViewBag.Subscriptions = Enum.GetValues<TransportSubscription>();
            ViewBag.Licenses      = Enum.GetValues<SwissLicense>();
            ViewBag.Relationships = Enum.GetValues<RelationshipLevel>();
        }
    }
}
