using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using OrderTrackingApp.Models;
using OrderTrackingApp.Filters;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;
using QuestPDF.Markdown;
using SkiaSharp;
using QuestPDF.SkiaSharpIntegration;

namespace OrderTrackingApp.Controllers
{
    public class ODGController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ODGController> _logger;

        public ODGController(
            AppDbContext context,
            UserManager<User> userManager,
            IWebHostEnvironment env,
            ILogger<ODGController> logger)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetForProject(int projectId)
        {
            var contacts = await _context.TroupeCastContacts
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

        [HasPermission("ODG.View")]
        public async Task<IActionResult> Index(int cinemaOrderId)
        {
            var list = await _context.ODGOrders
                .Where(o => o.CinemaOrderId == cinemaOrderId)
                .Include(o => o.TroupeOrari)
                .Include(o => o.CastConvocazioni)
                .Include(o => o.Trasporti)
                .Include(o => o.Contatti)
                .AsSplitQuery()
                .ToListAsync();

            ViewBag.CinemaOrderId = cinemaOrderId;
            return View(list);
        }

        [HttpGet]
        [HasPermission("ODG.Create")]
        public async Task<IActionResult> Create(int cinemaOrderId)
        {
            ViewBag.TroupeContacts = await _context.TroupeCastContacts
                .Where(c => c.CinemaOrderId == cinemaOrderId)
                .ToListAsync();

            ViewBag.CinemaOrderId = cinemaOrderId;

            return View(new ODGOrder { CinemaOrderId = cinemaOrderId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("ODG.Create")]
        public async Task<IActionResult> Create(ODGOrder odgOrder)
        {
            _logger.LogInformation("📥 Create POST - CinemaOrderId={Id}", odgOrder.CinemaOrderId);
            _logger.LogInformation("📋 Troupe={T}, Cast={C}, Trasporti={Tr}, Contatti={Co}",
                odgOrder.TroupeOrari?.Count ?? 0,
                odgOrder.CastConvocazioni?.Count ?? 0,
                odgOrder.Trasporti?.Count ?? 0,
                odgOrder.Contatti?.Count ?? 0);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("❌ ModelState non valido in Create:");
                foreach (var entry in ModelState.Where(e => e.Value?.Errors.Count > 0))
                {
                    foreach (var error in entry.Value!.Errors)
                    {
                        _logger.LogWarning("  - {Key}: {Error}", entry.Key, error.ErrorMessage);
                    }
                }

                ViewBag.CinemaOrderId = odgOrder.CinemaOrderId;
                ViewBag.TroupeContacts = await _context.TroupeCastContacts
                    .Where(c => c.CinemaOrderId == odgOrder.CinemaOrderId)
                    .ToListAsync();
                return View(odgOrder);
            }

            var user = await _userManager.GetUserAsync(User);
            odgOrder.CreatedBy = user?.VisualName ?? "Sconosciuto";
            odgOrder.TroupeOrari ??= new List<TroupeOrari>();
            odgOrder.CastConvocazioni ??= new List<CastConvocazioni>();
            odgOrder.Trasporti ??= new List<Trasporti>();
            odgOrder.Contatti ??= new List<Contatto>();

            _context.ODGOrders.Add(odgOrder);
            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ ODG creato Id={Id}, Troupe={T}, Cast={C}",
                odgOrder.Id,
                odgOrder.TroupeOrari.Count,
                odgOrder.CastConvocazioni.Count);

            return RedirectToAction(nameof(Index),
                new { cinemaOrderId = odgOrder.CinemaOrderId });
        }

        [HttpGet]
        [HasPermission("ODG.Edit")]
        public async Task<IActionResult> Edit(int id)
        {
            var odg = await _context.ODGOrders
                .Include(o => o.TroupeOrari)
                .Include(o => o.CastConvocazioni)
                .Include(o => o.Trasporti)
                .Include(o => o.Contatti)
                .AsSplitQuery()
                .FirstOrDefaultAsync(o => o.Id == id);

            if (odg == null)
                return NotFound();

            _logger.LogInformation("📖 Edit GET Id={Id}: Troupe={T}, Cast={C}, Trasporti={Tr}, Contatti={Co}",
                odg.Id,
                odg.TroupeOrari.Count,
                odg.CastConvocazioni.Count,
                odg.Trasporti.Count,
                odg.Contatti.Count);

            ViewBag.TroupeContacts = await _context.TroupeCastContacts
                .Where(c => c.CinemaOrderId == odg.CinemaOrderId)
                .ToListAsync();

            return View(odg);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("ODG.Edit")]
        public async Task<IActionResult> Edit(int id, ODGOrder odgOrder)
        {
            if (id != odgOrder.Id) return NotFound();

            _logger.LogInformation("📥 Edit POST Id={Id}", id);
            _logger.LogInformation("📋 Ricevuto - Troupe={T}, Cast={C}, Trasporti={Tr}, Contatti={Co}",
                odgOrder.TroupeOrari?.Count ?? 0,
                odgOrder.CastConvocazioni?.Count ?? 0,
                odgOrder.Trasporti?.Count ?? 0,
                odgOrder.Contatti?.Count ?? 0);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("❌ ModelState non valido in Edit:");
                foreach (var entry in ModelState.Where(e => e.Value?.Errors.Count > 0))
                {
                    foreach (var error in entry.Value!.Errors)
                    {
                        _logger.LogWarning("  - {Key}: {Error}", entry.Key, error.ErrorMessage);
                    }
                }

                ViewBag.TroupeContacts = await _context.TroupeCastContacts
                    .Where(c => c.CinemaOrderId == odgOrder.CinemaOrderId)
                    .ToListAsync();
                return View(odgOrder);
            }

            var existing = await _context.ODGOrders
                .Include(o => o.TroupeOrari)
                .Include(o => o.CastConvocazioni)
                .Include(o => o.Trasporti)
                .Include(o => o.Contatti)
                .AsSplitQuery()
                .FirstOrDefaultAsync(o => o.Id == id);

            if (existing == null) return NotFound();

            _logger.LogInformation("📋 DB prima dell'update - Troupe={T}, Cast={C}",
                existing.TroupeOrari.Count,
                existing.CastConvocazioni.Count);

            existing.DayRec = odgOrder.DayRec;
            existing.Film = odgOrder.Film;
            existing.Regista = odgOrder.Regista;
            existing.Produttore = odgOrder.Produttore;
            existing.Location = odgOrder.Location;
            existing.Meteo = odgOrder.Meteo;
            existing.SceneDaGirare = odgOrder.SceneDaGirare;
            existing.Catering = odgOrder.Catering;
            existing.ProntiAGirare = odgOrder.ProntiAGirare;
            existing.InizioRiprese = odgOrder.InizioRiprese;
            existing.PausaPranzo = odgOrder.PausaPranzo;
            existing.FineRiprese = odgOrder.FineRiprese;
            existing.TermineLavorazione = odgOrder.TermineLavorazione;
            existing.NoteProduzione = odgOrder.NoteProduzione;
            existing.NoteRegia = odgOrder.NoteRegia;
            existing.InformazioniUtili = odgOrder.InformazioniUtili;

            UpdateCollection(existing.TroupeOrari, odgOrder.TroupeOrari, _context.TroupeOrari, odgOrder.Id);
            UpdateCollection(existing.CastConvocazioni, odgOrder.CastConvocazioni, _context.CastConvocazioni, odgOrder.Id);
            UpdateCollection(existing.Trasporti, odgOrder.Trasporti, _context.Trasporti, odgOrder.Id);
            UpdateCollection(existing.Contatti, odgOrder.Contatti, _context.Contatti, odgOrder.Id);

            var user2 = await _userManager.GetUserAsync(User);
            existing.UpdatedBy = user2?.VisualName ?? "Sconosciuto";

            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ ODG aggiornato Id={Id}", existing.Id);

            return RedirectToAction(nameof(Index),
                new { cinemaOrderId = existing.CinemaOrderId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("ODG.Delete")]
        public async Task<IActionResult> Delete(int id)
        {
            var odg = await _context.ODGOrders
                .Include(o => o.CinemaOrder)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (odg == null) return NotFound();

            _context.ODGOrders.Remove(odg);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index),
                new { cinemaOrderId = odg.CinemaOrderId });
        }

        [HasPermission("ODG.Export")]
        public async Task<IActionResult> ExportPDF(int id)
        {
            var odgOrder = await _context.ODGOrders
                .Include(o => o.TroupeOrari)
                .Include(o => o.CastConvocazioni)
                .Include(o => o.Trasporti)
                .Include(o => o.Contatti)
                .AsSplitQuery()
                .FirstOrDefaultAsync(o => o.Id == id);
            if (odgOrder == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var exporterName = user?.VisualName ?? "Sconosciuto";

            using var stream = new MemoryStream();
            var logoPath = Path.Combine(_env.WebRootPath, "images", "logo_pj_nuovo.png");

            Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().ShowOnce().Column(header =>
                    {
                        header.Item().AlignCenter().Element(c =>
                        {
                            if (System.IO.File.Exists(logoPath))
                                c.MaxWidth(150).MaxHeight(150).Image(logoPath);
                        });
                        header.Item().PaddingTop(5).AlignCenter()
                              .Text("ORDINE DEL GIORNO").FontSize(24).Bold();
                    });

                    page.Content().Column(col =>
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cd =>
                            {
                                cd.ConstantColumn(150);
                                cd.RelativeColumn();
                            });

                            var rows = new (string, string?)[]
                            {
                                ("Giorno di Ripresa:",   odgOrder.DayRec),
                                ("Film:",                odgOrder.Film),
                                ("Regista:",             odgOrder.Regista),
                                ("Produttore:",          odgOrder.Produttore),
                                ("Location:",            odgOrder.Location),
                                ("Meteo:",               odgOrder.Meteo),
                                ("Scene da girare:",     odgOrder.SceneDaGirare),
                                ("Catering:",            odgOrder.Catering),
                                ("Convocazione sul Set:",odgOrder.ProntiAGirare),
                                ("Pronti a girare",      odgOrder.InizioRiprese),
                                ("Pausa:",               odgOrder.PausaPranzo),
                                ("Fine riprese:",        odgOrder.FineRiprese),
                                ("Termine lavorazione:", odgOrder.TermineLavorazione)
                            };

                            int i = 0;
                            foreach (var (lbl, val) in rows)
                            {
                                var bg = i++ % 2 == 0 ? Colors.White : Colors.Grey.Lighten2;
                                table.Cell().Background(bg).Border(1).BorderColor(Colors.Black)
                                     .Padding(5).Text(lbl).SemiBold();
                                table.Cell().Background(bg).Border(1).BorderColor(Colors.Black)
                                     .Padding(5).Text(val ?? "");
                            }
                        });

                        AddSectionMarkdown(col, "NOTE DI PRODUZIONE", odgOrder.NoteProduzione);
                        AddSectionMarkdown(col, "NOTE DI REGIA", odgOrder.NoteRegia);
                        AddSectionMarkdown(col, "INFORMAZIONI UTILI", odgOrder.InformazioniUtili);

                        AddTable(col, "TROUPE ORARI",
                                 new[] { "Nome", "Ruolo", "Orario" },
                                 odgOrder.TroupeOrari,
                                 t => new[] { t.Nome, t.Ruolo, t.Orario },
                                 paddingTop: 50);

                        AddTable(col, "CONVOCAZIONI",
                                 new[] { "Attore", "Convocazione", "Costume", "Trucco", "Pronti" },
                                 odgOrder.CastConvocazioni,
                                 c => new[] { c.Attore, c.PickUp, c.Costume, c.Trucco, c.Pronti });

                        AddTable(col, "TRASPORTI",
                                 new[] { "Auto", "Chi", "Dove", "Ora" },
                                 odgOrder.Trasporti,
                                 t => new[] { t.Auto, t.Chi, t.Dove, t.Ora });

                        AddTable(col, "CONTATTI IMPORTANTI",
                                 new[] { "Nome", "Ruolo", "Telefono" },
                                 odgOrder.Contatti,
                                 c => new[] { c.Nome, c.Ruolo, c.Email },
                                 emailColumnNoWrap: true);

                        col.Item()
                           .AlignBottom()
                           .AlignCenter()
                           .PaddingTop(20)
                           .Text($"Esportato il {DateTime.Now:dd/MM/yyyy HH:mm} da {exporterName}")
                           .FontSize(10)
                           .FontColor(Colors.Grey.Medium);
                    });
                });
            })
            .GeneratePdf(stream);

            stream.Position = 0;
            var pdfBytes = stream.ToArray();
            var pdfName = $"ODG_{odgOrder.Film}_{odgOrder.DayRec}.pdf";

            return new FileContentResult(pdfBytes, "application/pdf")
            {
                FileDownloadName = pdfName
            };
        }

        private void AddSectionMarkdown(ColumnDescriptor parent, string title, string? content)
        {
            if (string.IsNullOrWhiteSpace(content)) return;

            var md = Regex.Replace(content, @"<\s*p[^>]*>", "")
                          .Replace("</p>", "\n\n");
            md = Regex.Replace(md, @"<\s*ul[^>]*>|</ul>", "");
            md = Regex.Replace(md, @"<\s*ol[^>]*>|</ol>", "");
            md = Regex.Replace(md, @"<\s*li[^>]*>(.*?)</li>", "- $1\n", RegexOptions.Singleline);
            md = Regex.Replace(md, "<.*?>", "").Trim();

            parent.Item().PaddingTop(20).Column(col =>
            {
                col.Item().PaddingBottom(4).Text(title).FontSize(14).Bold();
                col.Item().Border(1).Background(Colors.Grey.Lighten4)
                   .Padding(10)
                   .Element(e => e.Markdown(md));
            });
        }

        private void AddTable<T>(
            ColumnDescriptor parent,
            string title,
            string[] headers,
            IEnumerable<T> items,
            Func<T, string[]> selector,
            float paddingTop = 15,
            bool emailColumnNoWrap = false)
        {
            if (!items.Any()) return;

            parent.Item().PaddingTop(paddingTop).Column(col =>
            {
                col.Item().PaddingBottom(4).Text(title).FontSize(14).Bold();
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(cd =>
                    {
                        for (int i = 0; i < headers.Length; i++)
                            cd.RelativeColumn((i == headers.Length - 1 && emailColumnNoWrap) ? 2 : 1);
                    });

                    table.Header(h =>
                    {
                        foreach (var htxt in headers)
                            h.Cell()
                             .Background(Colors.Blue.Medium)
                             .Border(1).BorderColor(Colors.Black)
                             .Padding(6).Text(htxt).FontColor(Colors.White).Bold();
                    });

                    int r = 0;
                    foreach (var itm in items)
                    {
                        var bg = (r++ % 2 == 0) ? Colors.White : Colors.Grey.Lighten4;
                        var cells = selector(itm);

                        for (int c = 0; c < cells.Length; c++)
                        {
                            table.Cell()
                                 .Background(bg)
                                 .Border(1).BorderColor(Colors.Black)
                                 .Padding(6)
                                 .Text(cells[c] ?? "");
                        }
                    }
                });
            });
        }

        private void UpdateCollection<T>(
            ICollection<T> existing,
            ICollection<T>? updated,
            DbSet<T> dbSet,
            int odgOrderId
        ) where T : class
        {
            updated ??= new List<T>();

            var idProp = typeof(T).GetProperty("Id");
            var fkProp = typeof(T).GetProperty("ODGOrderId");

            var updatedIds = updated.Select(u => idProp?.GetValue(u)).ToHashSet();
            foreach (var oldItem in existing.ToList())
            {
                var oldId = idProp?.GetValue(oldItem);
                if (!updatedIds.Contains(oldId))
                    dbSet.Remove(oldItem);
            }

            foreach (var item in updated)
            {
                fkProp?.SetValue(item, odgOrderId);

                var itemId = idProp?.GetValue(item);
                var match = existing.FirstOrDefault(e => idProp?.GetValue(e)?.Equals(itemId) == true);

                if (match == null)
                {
                    existing.Add(item);
                }
                else
                {
                    var props = typeof(T).GetProperties()
                                         .Where(p => p.Name != "Id" && p.Name != "ODGOrderId");

                    foreach (var p in props)
                    {
                        var newValue = p.GetValue(item);
                        p.SetValue(match, newValue);
                    }
                }
            }
        }
    }
}