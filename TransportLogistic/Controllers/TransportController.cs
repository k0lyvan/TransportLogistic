using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TransportLogistic.Data;
using TransportLogistic.Models;

namespace TransportLogistic.Controllers
{
    public class TransportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TransportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Transports - доступ всем авторизованным
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var transports = await _context.Transports.ToListAsync();
            return View(transports);
        }

        // GET: Transports/Details/5
        [Authorize]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var transport = await _context.Transports
                .FirstOrDefaultAsync(m => m.Id == id);

            if (transport == null) return NotFound();

            return View(transport);
        }

        // GET: Transports/Create - только Admin и Dispatcher
        [Authorize(Roles = "Admin,Dispatcher")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Transports/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Dispatcher")]
        public async Task<IActionResult> Create([Bind("Model,CarNumber,Capacity")] Transport transport)
        {
            if (ModelState.IsValid)
            {
                _context.Add(transport);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Транспорт успешно добавлен!";
                return RedirectToAction(nameof(Index));
            }
            return View(transport);
        }

        // GET: Transports/Edit/5 - только Admin и Dispatcher
        [Authorize(Roles = "Admin,Dispatcher")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var transport = await _context.Transports.FindAsync(id);
            if (transport == null) return NotFound();

            return View(transport);
        }

        // POST: Transports/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Dispatcher")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Model,CarNumber,Capacity")] Transport transport)
        {
            if (id != transport.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(transport);
                    await _context.SaveChangesAsync();
                    TempData["Message"] = "Транспорт успешно обновлен!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TransportExists(transport.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(transport);
        }

        // GET: Transports/Delete/5 - только Admin
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var transport = await _context.Transports
                .FirstOrDefaultAsync(m => m.Id == id);

            if (transport == null) return NotFound();

            return View(transport);
        }

        // POST: Transports/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var transport = await _context.Transports.FindAsync(id);
            if (transport != null)
            {
                // Проверяем, есть ли связанные рейсы
                var hasTrips = await _context.Trips.AnyAsync(t => t.Transport == id);
                if (hasTrips)
                {
                    TempData["Error"] = "Нельзя удалить транспорт, так как он используется в рейсах!";
                    return RedirectToAction(nameof(Index));
                }

                _context.Transports.Remove(transport);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Транспорт успешно удален!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool TransportExists(int id)
        {
            return _context.Transports.Any(e => e.Id == id);
        }
    }
}