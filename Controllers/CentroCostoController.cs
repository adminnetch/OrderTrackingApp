using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderTrackingApp.Models;
using OrderTrackingApp.Filters;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using OfficeOpenXml;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Drawing;
using QuestPDF.Previewer;

namespace OrderTrackingApp.Controllers
{
    
    public class CentroCostoController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public CentroCostoController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ✅ INDEX
        [HasPermission("Finanze.View")]
        public async Task<IActionResult> Index(int cinemaOrderId, string? mese)
        {
            var progetto = await _context.CinemaOrders.FindAsync(cinemaOrderId);
            if (progetto == null) return NotFound("Progetto non trovato");

            var centro = await _context.CentriCosto
                .Include(c => c.Spese)
                .FirstOrDefaultAsync(c => c.CinemaOrderId == cinemaOrderId);

            if (centro == null)
            {
                centro = new CentroCosto
                {
                    Nome = progetto.Title,
                    CinemaOrderId = cinemaOrderId
                };

                _context.CentriCosto.Add(centro);
                await _context.SaveChangesAsync();
            }

            if (!string.IsNullOrEmpty(mese) && DateTime.TryParse($"{mese}-01", out var meseParsed))
            {
                centro.Spese = centro.Spese
                    .Where(s => s.Data.Month == meseParsed.Month && s.Data.Year == meseParsed.Year)
                    .ToList();
            }

            ViewBag.Categorie = centro.Spese
                .GroupBy(s => s.Tipo)
                .Select(g => g.Key)
                .ToList();

            ViewBag.Valori = centro.Spese
                .GroupBy(s => s.Tipo)
                .Select(g => g.Sum(x => x.Importo))
                .ToList();

            ViewBag.CinemaOrderId = cinemaOrderId;
            return View(centro);
        }

        // ✅ CREATE GET
        [HttpGet]
        [HasPermission("Finanze.Create")]
        public IActionResult CreateSpesa(int centroCostoId)
        {
            var spesa = new VoceSpesa
            {
                Data = DateTime.Today,
                CentroCostoId = centroCostoId
            };
            return View(spesa);
        }

        // ✅ CREATE POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Finanze.Create")]
        public async Task<IActionResult> CreateSpesa([FromForm] VoceSpesa spesa, IFormFile? scontrino)
        {
            if (ModelState.IsValid)
            {
                var centro = await _context.CentriCosto.FindAsync(spesa.CentroCostoId);
                if (centro == null) return NotFound();

                var folder = Path.Combine("/home/operation/OTA/OrderTrackingApp/Finanze",
                    centro.Nome, spesa.Data.ToString("MM"), spesa.Tipo.Replace(" ", "_"));

                Directory.CreateDirectory(folder);

                if (scontrino != null && scontrino.Length > 0)
                {
                    var fileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{Path.GetFileName(scontrino.FileName)}";
                    var fullPath = Path.Combine(folder, fileName);

                    using var stream = new FileStream(fullPath, FileMode.Create);
                    await scontrino.CopyToAsync(stream);
                    spesa.ScontrinoPath = fullPath;
                }

                _context.VociSpesa.Add(spesa);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index", new { cinemaOrderId = centro.CinemaOrderId });
            }

            return View(spesa);
        }

        // ✅ DETAILS
        [HttpGet]
        [HasPermission("Finanze.Details")]
        public async Task<IActionResult> Details(int id)
        {
            var spesa = await _context.VociSpesa
                .Include(s => s.CentroCosto)
                .ThenInclude(cc => cc.CinemaOrder)
                .FirstOrDefaultAsync(s => s.Id == id);

            return spesa == null ? NotFound() : View(spesa);
        }

        // ✅ EDIT GET
        [HttpGet]
        [HasPermission("Finanze.Edit")]
        public async Task<IActionResult> EditSpesa(int id)
        {
            var spesa = await _context.VociSpesa
                .Include(s => s.CentroCosto)
                .ThenInclude(c => c.CinemaOrder)
                .FirstOrDefaultAsync(s => s.Id == id);

            return spesa == null ? NotFound() : View(spesa);
        }

        // ✅ EDIT POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Finanze.Edit")]
        public async Task<IActionResult> EditSpesa(VoceSpesa spesa, IFormFile? scontrino)
        {
            if (ModelState.IsValid)
            {
                var existing = await _context.VociSpesa
                    .Include(s => s.CentroCosto)
                    .FirstOrDefaultAsync(s => s.Id == spesa.Id);

                if (existing == null) return NotFound();

                existing.Data = spesa.Data;
                existing.Tipo = spesa.Tipo;
                existing.Importo = spesa.Importo;
                existing.Nota = spesa.Nota;

                if (scontrino != null && scontrino.Length > 0)
                {
                    var folder = Path.Combine("/home/operation/OTA/OrderTrackingApp/Finanze",
                        existing.CentroCosto.Nome, existing.Data.ToString("MM"), existing.Tipo.Replace(" ", "_"));

                    Directory.CreateDirectory(folder);

                    var fileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{Path.GetFileName(scontrino.FileName)}";
                    var fullPath = Path.Combine(folder, fileName);

                    using var stream = new FileStream(fullPath, FileMode.Create);
                    await scontrino.CopyToAsync(stream);
                    existing.ScontrinoPath = fullPath;
                }

                await _context.SaveChangesAsync();
                return RedirectToAction("Index", new { cinemaOrderId = existing.CentroCosto.CinemaOrderId });
            }

            return View(spesa);
        }

        // ✅ DOWNLOAD SCONTRINO
        [HasPermission("Finanze.Download")]
        public IActionResult DownloadScontrino(int id)
        {
            var spesa = _context.VociSpesa.Find(id);
            if (spesa == null || string.IsNullOrEmpty(spesa.ScontrinoPath) || !System.IO.File.Exists(spesa.ScontrinoPath))
                return NotFound("File non trovato");

            var fileBytes = System.IO.File.ReadAllBytes(spesa.ScontrinoPath);
            var fileName = Path.GetFileName(spesa.ScontrinoPath);
            return File(fileBytes, "application/octet-stream", fileName);
        }

        // ✅ DELETE
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Finanze.Delete")]
        public async Task<IActionResult> Delete(int id)
        {
            var spesa = await _context.VociSpesa.FindAsync(id);
            if (spesa == null) return NotFound();

            if (!string.IsNullOrEmpty(spesa.ScontrinoPath) && System.IO.File.Exists(spesa.ScontrinoPath))
            {
                try
                {
                    System.IO.File.Delete(spesa.ScontrinoPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERRORE] Impossibile eliminare lo scontrino: {ex.Message}");
                }
            }

            var centroId = spesa.CentroCostoId;

            _context.VociSpesa.Remove(spesa);
            await _context.SaveChangesAsync();

            var cinemaOrderId = await _context.CentriCosto
                .Where(c => c.Id == centroId)
                .Select(c => c.CinemaOrderId)
                .FirstOrDefaultAsync();

            return RedirectToAction("Index", new { cinemaOrderId });
        }

        // ✅ ESPORTA
        // ✅ ESPORTA
        [HasPermission("Finanze.Export")]
        public async Task<IActionResult> Esporta(int cinemaOrderId, string formato = "csv")
        {
            var centro = await _context.CentriCosto
                .Include(c => c.Spese)
                .FirstOrDefaultAsync(c => c.CinemaOrderId == cinemaOrderId);

            if (centro == null || centro.Spese.Count == 0)
                return Content("Nessuna spesa da esportare.");

            var spese = centro.Spese.OrderBy(s => s.Data).ToList();
            var nomeFileBase = $"Rendiconto_{centro.Nome}_{DateTime.Now:yyyyMMdd}";

            if (formato == "csv")
            {
                var csv = new StringBuilder();
                csv.AppendLine("Data;Tipo;Importo;Nota");

                foreach (var s in spese)
                    csv.AppendLine($"{s.Data:dd/MM/yyyy};{s.Tipo};{s.Importo:F2};{s.Nota?.Replace(";", " ")}");

                return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", nomeFileBase + ".csv");
            }

            if (formato == "excel")
            {
                using var package = new ExcelPackage();
                var ws = package.Workbook.Worksheets.Add("Rendiconto");

                ws.Cells[1, 1].Value = "Data";
                ws.Cells[1, 2].Value = "Tipo";
                ws.Cells[1, 3].Value = "Importo";
                ws.Cells[1, 4].Value = "Nota";

                for (int i = 0; i < spese.Count; i++)
                {
                    ws.Cells[i + 2, 1].Value = spese[i].Data.ToString("dd/MM/yyyy");
                    ws.Cells[i + 2, 2].Value = spese[i].Tipo;
                    ws.Cells[i + 2, 3].Value = spese[i].Importo;
                    ws.Cells[i + 2, 4].Value = spese[i].Nota;
                }

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nomeFileBase + ".xlsx");
            }

            if (formato == "pdf")
            {
                var stream = new MemoryStream();
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(30);
                        page.DefaultTextStyle(x => x.FontSize(11));

                        // Header
                        page.Header().Element(header =>
                        {
                            header.Column(col =>
                            {
                                col.Item().AlignCenter().Text($"Rendiconto Spese - {centro.Nome}")
                                    .FontSize(20).Bold();

                                col.Item().Height(10); // Spazio sotto al titolo
                            });
                        });

                        // Contenuto - tabella spese
                        page.Content().Element(content =>
                        {
                            content.Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(90); // Data
                                    columns.RelativeColumn();   // Tipo
                                    columns.ConstantColumn(80); // Importo
                                    columns.RelativeColumn();   // Nota
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(CellStyle).Text("Data").Bold();
                                    header.Cell().Element(CellStyle).Text("Tipo").Bold();
                                    header.Cell().Element(CellStyle).Text("Importo").Bold();
                                    header.Cell().Element(CellStyle).Text("Nota").Bold();
                                });

                                foreach (var s in spese)
                                {
                                    table.Cell().Element(CellStyle).Text(s.Data.ToString("dd/MM/yyyy"));
                                    table.Cell().Element(CellStyle).Text(s.Tipo);
                                    table.Cell().Element(CellStyle).Text($"{s.Importo:F2} CHF");
                                    table.Cell().Element(CellStyle).Text(s.Nota ?? "");
                                }

                                static IContainer CellStyle(IContainer container) => container
                                    .BorderBottom(1)
                                    .BorderColor(Colors.Grey.Lighten2)
                                    .PaddingVertical(4)
                                    .PaddingHorizontal(2);
                            });
                        });

                        // Footer
                        page.Footer().AlignRight().Text($"Esportato il {DateTime.Now:dd/MM/yyyy HH:mm}")
                            .FontSize(10)
                            .FontColor(Colors.Grey.Medium);
                    });
                }).GeneratePdf(stream);

                stream.Position = 0;
                return File(stream.ToArray(), "application/pdf", nomeFileBase + ".pdf");
            }

            // Fallback se il formato non è valido
            return BadRequest("Formato non supportato.");
        }
    }
}
