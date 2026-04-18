using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TransportLogistic.Data;
using TransportLogistic.Models;

namespace TransportLogistic.Controllers
{
    public class TripsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public TripsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Trips
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var currentUser = User.Identity?.Name;
            var userRole = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;

            IQueryable<Trip> trips = _context.Trips
                .Include(t => t.RouteNavigation)
                .Include(t => t.TransportNavigation)
                .Include(t => t.DriverNavigation)
                .Include(t => t.ConductorNavigation);

            if (userRole == "Driver")
            {
                trips = trips.Where(t => t.Driver == currentUser);
            }
            else if (userRole == "Conductor")
            {
                trips = trips.Where(t => t.Conductor == currentUser);
            }

            return View(await trips.ToListAsync());
        }

        // GET: Trips/Details/5
        [Authorize]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var trip = await _context.Trips
                .Include(t => t.RouteNavigation)
                    .ThenInclude(r => r.StartNavigation)
                .Include(t => t.RouteNavigation)
                    .ThenInclude(r => r.StopNavigation)
                .Include(t => t.TransportNavigation)
                .Include(t => t.DriverNavigation)
                .Include(t => t.ConductorNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (trip == null) return NotFound();

            var currentUser = User.Identity?.Name;
            var userRole = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;

            if ((userRole == "Driver" && trip.Driver != currentUser) ||
                (userRole == "Conductor" && trip.Conductor != currentUser))
            {
                return Forbid();
            }

            return View(trip);
        }

        // GET: Trips/Create
        [Authorize(Roles = "Admin,Dispatcher")]
        public async Task<IActionResult> Create()
        {
            await FillSelectLists();
            return View();
        }

        // POST: Trips/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Dispatcher")]
        public async Task<IActionResult> Create([Bind("Route,DepatureTime,ArrivalTime,Transport,Driver,Conductor,Price")] Trip trip)  // ← ДОБАВИЛИ Price
        {

            ModelState.Remove("DriverNavigation");
            ModelState.Remove("ConductorNavigation");
            ModelState.Remove("RouteNavigation");
            ModelState.Remove("TransportNavigation");
            ModelState.Remove("Orders");

            if (ModelState.IsValid)
            {

                if (trip.DepatureTime >= trip.ArrivalTime)
                {
                    ModelState.AddModelError("DepatureTime", "Время отправления должно быть раньше времени прибытия");
                    await FillSelectLists(trip.Route, trip.Transport, trip.Driver, trip.Conductor);
                    return View(trip);
                }


                if (trip.Price <= 0)
                {
                    ModelState.AddModelError("Price", "Цена должна быть больше 0");
                    await FillSelectLists(trip.Route, trip.Transport, trip.Driver, trip.Conductor);
                    return View(trip);
                }


                if (!string.IsNullOrEmpty(trip.Driver))
                {
                    var driverExists = await _context.Users.AnyAsync(u => u.Id == trip.Driver);
                    if (!driverExists)
                    {
                        ModelState.AddModelError("Driver", "Выбранный водитель не существует");
                        await FillSelectLists(trip.Route, trip.Transport, trip.Driver, trip.Conductor);
                        return View(trip);
                    }
                }


                if (!string.IsNullOrEmpty(trip.Conductor))
                {
                    var conductorExists = await _context.Users.AnyAsync(u => u.Id == trip.Conductor);
                    if (!conductorExists)
                    {
                        ModelState.AddModelError("Conductor", "Выбранный кондуктор не существует");
                        await FillSelectLists(trip.Route, trip.Transport, trip.Driver, trip.Conductor);
                        return View(trip);
                    }
                }

                _context.Add(trip);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Рейс успешно создан!";
                return RedirectToAction(nameof(Index));
            }


            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                Console.WriteLine($"Validation Error: {error.ErrorMessage}");
            }

            await FillSelectLists(trip.Route, trip.Transport, trip.Driver, trip.Conductor);
            return View(trip);
        }

        // GET: Trips/Edit/5
        [Authorize(Roles = "Admin,Dispatcher")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var trip = await _context.Trips.FindAsync(id);
            if (trip == null) return NotFound();

            await FillSelectLists(trip.Route, trip.Transport, trip.Driver, trip.Conductor);

            return View(trip);
        }

        // POST: Trips/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Dispatcher")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Route,DepatureTime,ArrivalTime,Transport,Driver,Conductor,Price")] Trip trip)  // ← ДОБАВИЛИ Price
        {
            if (id != trip.Id) return NotFound();


            ModelState.Remove("DriverNavigation");
            ModelState.Remove("ConductorNavigation");
            ModelState.Remove("RouteNavigation");
            ModelState.Remove("TransportNavigation");
            ModelState.Remove("Orders");

            if (ModelState.IsValid)
            {
                // Проверяем цену
                if (trip.Price <= 0)
                {
                    ModelState.AddModelError("Price", "Цена должна быть больше 0");
                    await FillSelectLists(trip.Route, trip.Transport, trip.Driver, trip.Conductor);
                    return View(trip);
                }

                try
                {
                    _context.Update(trip);
                    await _context.SaveChangesAsync();
                    TempData["Message"] = "Рейс успешно обновлен!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TripExists(trip.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            await FillSelectLists(trip.Route, trip.Transport, trip.Driver, trip.Conductor);
            return View(trip);
        }

        // GET: Trips/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var trip = await _context.Trips
                .Include(t => t.RouteNavigation)
                .Include(t => t.TransportNavigation)
                .Include(t => t.DriverNavigation)
                .Include(t => t.ConductorNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (trip == null) return NotFound();

            return View(trip);
        }

        // POST: Trips/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var trip = await _context.Trips.FindAsync(id);
            if (trip != null)
            {
                var hasOrders = await _context.Orders.AnyAsync(o => o.Trip == id);
                if (hasOrders)
                {
                    TempData["Error"] = "Нельзя удалить рейс с существующими заказами!";
                    return RedirectToAction(nameof(Index));
                }

                _context.Trips.Remove(trip);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Рейс успешно удален!";
            }

            return RedirectToAction(nameof(Index));
        }


        private async Task FillSelectLists(int? selectedRoute = null, int? selectedTransport = null,
                                          string? selectedDriver = null, string? selectedConductor = null)
        {

            ViewBag.Routes = new SelectList(
                await _context.Routes.ToListAsync(),
                "Id", "Name", selectedRoute
            );


            ViewBag.Transports = new SelectList(
                await _context.Transports.ToListAsync(),
                "Id", "Model", selectedTransport
            );


            var drivers = await (
                from user in _context.Users
                join userRole in _context.UserRoles on user.Id equals userRole.UserId
                join role in _context.Roles on userRole.RoleId equals role.Id
                where role.Name == "Driver"
                select new { user.Id, DisplayName = user.UserName ?? user.Email }
            ).ToListAsync();

            ViewBag.Drivers = new SelectList(
                drivers,
                "Id",
                "DisplayName",
                selectedDriver
            );


            var conductors = await (
                from user in _context.Users
                join userRole in _context.UserRoles on user.Id equals userRole.UserId
                join role in _context.Roles on userRole.RoleId equals role.Id
                where role.Name == "Conductor"
                select new { user.Id, DisplayName = user.UserName ?? user.Email }
            ).ToListAsync();

            ViewBag.Conductors = new SelectList(
                conductors,
                "Id",
                "DisplayName",
                selectedConductor
            );
        }

        private bool TripExists(int id)
        {
            return _context.Trips.Any(e => e.Id == id);
        }
    }
}