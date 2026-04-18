using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportLogistic.Data;
using TransportLogistic.Models;

namespace TransportLogistic.Controllers
{
    public class CitiesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CitiesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Cities 
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var cities = await _context.Cities.ToListAsync();
            return View(cities);
        }

        // GET: Cities/Details/5
        [Authorize]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var city = await _context.Cities
                .FirstOrDefaultAsync(m => m.Id == id);

            if (city == null) return NotFound();

            return View(city);
        }

        // GET: Cities/Create 
        [Authorize(Roles = "Admin,Dispatcher")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Cities/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Dispatcher")]
        public async Task<IActionResult> Create([Bind("Name,Region")] City city)
        {
            if (ModelState.IsValid)
            {
                _context.Add(city);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Город успешно добавлен!";
                return RedirectToAction(nameof(Index));
            }
            return View(city);
        }

        // GET: Cities/Edit/5
        [Authorize(Roles = "Admin,Dispatcher")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var city = await _context.Cities.FindAsync(id);
            if (city == null) return NotFound();

            return View(city);
        }

        // POST: Cities/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Dispatcher")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Region")] City city)
        {
            if (id != city.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(city);
                    await _context.SaveChangesAsync();
                    TempData["Message"] = "Город успешно обновлен!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CityExists(city.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(city);
        }

        // GET: Cities/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var city = await _context.Cities
                .FirstOrDefaultAsync(m => m.Id == id);

            if (city == null) return NotFound();

            return View(city);
        }

        // POST: Cities/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var city = await _context.Cities.FindAsync(id);
            if (city != null)
            {
                // Проверяем, есть ли связанные маршруты
                var hasRoutesAsStart = await _context.Routes.AnyAsync(r => r.Start == id);
                var hasRoutesAsStop = await _context.Routes.AnyAsync(r => r.Stop == id);

                if (hasRoutesAsStart || hasRoutesAsStop)
                {
                    TempData["Error"] = "Нельзя удалить город, так как он используется в маршрутах!";
                    return RedirectToAction(nameof(Index));
                }

                _context.Cities.Remove(city);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Город успешно удален!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool CityExists(int id)
        {
            return _context.Cities.Any(e => e.Id == id);
        }
    }
}