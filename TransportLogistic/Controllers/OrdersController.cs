using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TransportLogistic.Data;
using TransportLogistic.Models;

namespace TransportLogistic.Controllers
{
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public OrdersController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Orders
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var currentUser = User.Identity?.Name;
            var userRole = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;

            IQueryable<Order> orders = _context.Orders
                .Include(o => o.TripNavigation)
                    .ThenInclude(t => t.RouteNavigation)
                        .ThenInclude(r => r.StartNavigation)
                .Include(o => o.TripNavigation)
                    .ThenInclude(t => t.RouteNavigation)
                        .ThenInclude(r => r.StopNavigation)
                .Include(o => o.TripNavigation)
                    .ThenInclude(t => t.TransportNavigation)
                .Include(o => o.UserNavigation);

            // Разные роли видят разные заказы
            if (userRole == "User")
            {
                orders = orders.Where(o => o.User == currentUser);
            }
            else if (userRole == "Driver" || userRole == "Conductor")
            {
                orders = orders.Where(o => o.TripNavigation.Driver == currentUser ||
                                           o.TripNavigation.Conductor == currentUser);
            }

            return View(await orders.ToListAsync());
        }

        // GET: Orders/Details/5
        [Authorize]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders
                .Include(o => o.TripNavigation)
                    .ThenInclude(t => t.RouteNavigation)
                        .ThenInclude(r => r.StartNavigation)
                .Include(o => o.TripNavigation)
                    .ThenInclude(t => t.RouteNavigation)
                        .ThenInclude(r => r.StopNavigation)
                .Include(o => o.TripNavigation)
                    .ThenInclude(t => t.TransportNavigation)
                .Include(o => o.UserNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null) return NotFound();

            // Проверка доступа
            var currentUser = User.Identity?.Name;
            var userRole = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;

            if (userRole == "User" && order.User != currentUser)
            {
                return Forbid();
            }

            if ((userRole == "Driver" || userRole == "Conductor") &&
                order.TripNavigation.Driver != currentUser &&
                order.TripNavigation.Conductor != currentUser)
            {
                return Forbid();
            }

            return View(order);
        }

        // GET: Orders/Create
        [Authorize]
        public async Task<IActionResult> Create()
        {
            // Только будущие рейсы с свободными местами
            var availableTrips = await _context.Trips
                .Include(t => t.RouteNavigation)
                .Include(t => t.TransportNavigation)
                .Where(t => t.DepatureTime > DateTime.Now)
                .ToListAsync();

            ViewBag.Trips = new SelectList(availableTrips, "Id", "RouteNavigation.Name");
            return View();
        }

        // POST: Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create([Bind("Trip,SeatNumber")] Order order)
        {
            order.User = User.Identity?.Name;
            order.Stasus = "Ожидает подтверждения";

            // Проверка цены (можно вычислить из расстояния)
            var trip = await _context.Trips
                .Include(t => t.RouteNavigation)
                .FirstOrDefaultAsync(t => t.Id == order.Trip);

            if (trip != null)
            {
                // Цена = расстояние * 10 (например)
                order.Price = (decimal)trip.RouteNavigation.Distance * 10m;
            }

            // Проверка свободного места
            var existingOrder = await _context.Orders
                .FirstOrDefaultAsync(o => o.Trip == order.Trip && o.SeatNumber == order.SeatNumber);

            if (existingOrder != null)
            {
                ModelState.AddModelError("SeatNumber", "Это место уже занято!");
                var availableTrips = await _context.Trips
                    .Include(t => t.RouteNavigation)
                    .Where(t => t.DepatureTime > DateTime.Now)
                    .ToListAsync();
                ViewBag.Trips = new SelectList(availableTrips, "Id", "RouteNavigation.Name");
                return View(order);
            }

            if (ModelState.IsValid)
            {
                _context.Add(order);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Заказ успешно создан!";
                return RedirectToAction(nameof(Index));
            }

            var trips = await _context.Trips
                .Include(t => t.RouteNavigation)
                .Where(t => t.DepatureTime > DateTime.Now)
                .ToListAsync();
            ViewBag.Trips = new SelectList(trips, "Id", "RouteNavigation.Name");
            return View(order);
        }

        // GET: Orders/Edit/5 - только Admin и Dispatcher
        [Authorize(Roles = "Admin,Dispatcher")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders
                .Include(o => o.TripNavigation)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            var trips = await _context.Trips
                .Include(t => t.RouteNavigation)
                .Where(t => t.DepatureTime > DateTime.Now)
                .ToListAsync();

            ViewBag.Trips = new SelectList(trips, "Id", "RouteNavigation.Name", order.Trip);
            return View(order);
        }

        // POST: Orders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Dispatcher")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Trip,SeatNumber,Price,Stasus")] Order order)
        {
            if (id != order.Id) return NotFound();

            order.User = User.Identity?.Name;

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(order);
                    await _context.SaveChangesAsync();
                    TempData["Message"] = "Заказ успешно обновлен!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(order.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            var trips = await _context.Trips
                .Include(t => t.RouteNavigation)
                .Where(t => t.DepatureTime > DateTime.Now)
                .ToListAsync();
            ViewBag.Trips = new SelectList(trips, "Id", "RouteNavigation.Name", order.Trip);
            return View(order);
        }

        // GET: Orders/Delete/5 - только Admin
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders
                .Include(o => o.TripNavigation)
                    .ThenInclude(t => t.RouteNavigation)
                .Include(o => o.UserNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Заказ успешно удален!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.Id == id);
        }
    }
}