using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderTrackingApp.Models;
using OrderTrackingApp.Filters;
using System.Linq;
using System.Threading.Tasks;

namespace OrderTrackingApp.Controllers
{
    public class PianoDiLavorazioneController : Controller
    {
        private readonly AppDbContext _context;

        public PianoDiLavorazioneController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [HasPermission("Piani.View")]
        public async Task<IActionResult> Index(int cinemaOrderId)
        {
            var piani = await _context.PianiDiLavorazione
                .Where(p => p.CinemaOrderId == cinemaOrderId)
                .Include(p => p.GiorniRipresa)
                    .ThenInclude(g => g.Scene)
                .Include(p => p.GiorniRipresa)
                    .ThenInclude(g => g.Attori)
                .Include(p => p.GiorniRipresa)
                    .ThenInclude(g => g.Locations)
                .ToListAsync();

            ViewBag.CinemaOrderId = cinemaOrderId;
            return View(piani);
        }

        [HttpGet]
        [HasPermission("Piani.Create")]
        public IActionResult Create(int cinemaOrderId)
        {
            var piano = new PianoDiLavorazione
            {
                CinemaOrderId = cinemaOrderId
            };
            ViewBag.CinemaOrderId = cinemaOrderId;
            return View(piano);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Piani.Create")]
        public async Task<IActionResult> Create(PianoDiLavorazione piano, int cinemaOrderId)
        {
            if (piano.CinemaOrderId == 0 && cinemaOrderId != 0)
            {
                piano.CinemaOrderId = cinemaOrderId;
            }

            piano.GiorniRipresa ??= new List<GiornoRipresa>();

            foreach (var giorno in piano.GiorniRipresa)
            {
                giorno.PianoDiLavorazioneId = piano.Id; // ✅ collega ogni Giorno al Piano
                giorno.Scene ??= new List<ScenaRipresa>();
                giorno.Attori ??= new List<AttoreRipresa>();
                giorno.Locations ??= new List<LocationRipresa>();

                foreach (var scena in giorno.Scene)
                {
                    scena.GiornoRipresaId = giorno.Id; // ✅ collega ogni Scena al Giorno
                }
            }

            if (ModelState.IsValid)
            {
                _context.PianiDiLavorazione.Add(piano);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", new { cinemaOrderId = piano.CinemaOrderId });
            }

            ViewBag.CinemaOrderId = piano.CinemaOrderId;
            return View(piano);
        }





        [HttpGet]
        [HasPermission("Piani.Edit")]
        public async Task<IActionResult> Edit(int id)
        {
            var piano = await _context.PianiDiLavorazione
                .Include(p => p.GiorniRipresa)
                    .ThenInclude(g => g.Scene)
                .Include(p => p.GiorniRipresa)
                    .ThenInclude(g => g.Attori)
                .Include(p => p.GiorniRipresa)
                    .ThenInclude(g => g.Locations)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (piano == null)
                return NotFound();

            return View(piano);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Piani.Edit")]
        public async Task<IActionResult> Edit(PianoDiLavorazione piano)
        {
            if (ModelState.IsValid)
            {
                var existing = await _context.PianiDiLavorazione
                    .Include(p => p.GiorniRipresa)
                    .FirstOrDefaultAsync(p => p.Id == piano.Id);

                if (existing == null)
                    return NotFound();

                existing.TitoloCortometraggio = piano.TitoloCortometraggio;
                existing.NomeProduzione = piano.NomeProduzione;
                existing.Regista = piano.Regista;
                existing.Produttore = piano.Produttore;
                existing.Note = piano.Note;
                existing.LastUpdated = DateTime.Now;

                await _context.SaveChangesAsync();
                return RedirectToAction("Index", new { cinemaOrderId = existing.CinemaOrderId });
            }
            return View(piano);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Piani.Delete")]
        public async Task<IActionResult> Delete(int id)
        {
            var piano = await _context.PianiDiLavorazione
                .Include(p => p.GiorniRipresa)
                    .ThenInclude(g => g.Scene)
                .Include(p => p.GiorniRipresa)
                    .ThenInclude(g => g.Attori)
                .Include(p => p.GiorniRipresa)
                    .ThenInclude(g => g.Locations)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (piano == null)
                return NotFound();

            _context.PianiDiLavorazione.Remove(piano);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", new { cinemaOrderId = piano.CinemaOrderId });
        }
    }
}
