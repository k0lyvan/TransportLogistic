using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TransportLogistic.Data;
using TransportLogistic.Models;

namespace TransportLogistic.Controllers
{
    public class RoutesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RoutesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Routes - доступ всем авторизованным
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var routes = await _context.Routes
                .Include(r => r.StartNavigation)
                .Include(r => r.StopNavigation)
                .ToListAsync();
            return View(routes);
        }

        // GET: Routes/Details/5
        [Authorize]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var route = await _context.Routes
                .Include(r => r.StartNavigation)
                .Include(r => r.StopNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (route == null) return NotFound();

            return View(route);
        }

        // GET: Routes/Create - только Admin и Dispatcher
        [Authorize(Roles = "Admin,Dispatcher")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Cities = new SelectList(await _context.Cities.ToListAsync(), "Id", "Name");
            return View();
        }

        // POST: Routes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Dispatcher")]
        public async Task<IActionResult> Create([Bind("Name,Start,Stop,Distance")] TransportLogistic.Models.Route route)
        {
            if (ModelState.IsValid)
            {
                // Проверяем, что начальный и конечный города разные
                if (route.Start == route.Stop)
                {
                    ModelState.AddModelError("Stop", "Начальный и конечный города должны быть разными");
                    ViewBag.Cities = new SelectList(await _context.Cities.ToListAsync(), "Id", "Name");
                    return View(route);
                }

                _context.Add(route);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Маршрут успешно добавлен!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Cities = new SelectList(await _context.Cities.ToListAsync(), "Id", "Name");
            return View(route);
        }

        // GET: Routes/Edit/5
        [Authorize(Roles = "Admin,Dispatcher")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var route = await _context.Routes.FindAsync(id);
            if (route == null) return NotFound();

            ViewBag.Cities = new SelectList(await _context.Cities.ToListAsync(), "Id", "Name");
            return View(route);
        }

        // POST: Routes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Dispatcher")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Start,Stop,Distance")] TransportLogistic.Models.Route route)
        {
            if (id != route.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Проверяем, что начальный и конечный города разные
                    if (route.Start == route.Stop)
                    {
                        ModelState.AddModelError("Stop", "Начальный и конечный города должны быть разными");
                        ViewBag.Cities = new SelectList(await _context.Cities.ToListAsync(), "Id", "Name");
                        return View(route);
                    }

                    _context.Update(route);
                    await _context.SaveChangesAsync();
                    TempData["Message"] = "Маршрут успешно обновлен!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RouteExists(route.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Cities = new SelectList(await _context.Cities.ToListAsync(), "Id", "Name");
            return View(route);
        }

        // GET: Routes/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var route = await _context.Routes
                .Include(r => r.StartNavigation)
                .Include(r => r.StopNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (route == null) return NotFound();

            return View(route);
        }

        // POST: Routes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var route = await _context.Routes.FindAsync(id);
            if (route != null)
            {
                // Проверяем, есть ли связанные рейсы
                var hasTrips = await _context.Trips.AnyAsync(t => t.Route == id);
                if (hasTrips)
                {
                    TempData["Error"] = "Нельзя удалить маршрут, так как он используется в рейсах!";
                    return RedirectToAction(nameof(Index));
                }

                _context.Routes.Remove(route);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Маршрут успешно удален!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool RouteExists(int id)
        {
            return _context.Routes.Any(e => e.Id == id);
        }
    }
}