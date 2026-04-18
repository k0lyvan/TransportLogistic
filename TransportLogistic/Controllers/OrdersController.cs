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
            var currentUser = await _userManager.GetUserAsync(User);
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

            if (userRole == "User")
            {
                orders = orders.Where(o => o.User == currentUser.Id);
            }
            else if (userRole == "Driver" || userRole == "Conductor")
            {                
                orders = orders.Where(o => o.TripNavigation.Driver == currentUser.UserName ||
                                           o.TripNavigation.Conductor == currentUser.UserName);
            }

            return View(await orders.ToListAsync());
        }
        [Authorize]
        public async Task<IActionResult> Debug()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var debug = new
            {
                UserId = currentUser?.Id,
                UserName = currentUser?.UserName,
                UserEmail = currentUser?.Email,
                Roles = User.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role).Select(c => c.Value)
            };

            return Json(debug);
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

            var currentUser = await _userManager.GetUserAsync(User);
            var userRole = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;

            if (userRole == "User" && order.User != currentUser.Id)
            {
                return Forbid();
            }

            if ((userRole == "Driver" || userRole == "Conductor") &&
                order.TripNavigation.Driver != currentUser.UserName &&
                order.TripNavigation.Conductor != currentUser.UserName)
            {
                return Forbid();
            }

            return View(order);
        }

        // GET: Orders/Create
        [Authorize]
        public async Task<IActionResult> Create()
        {
            try
            {
                var availableTrips = await _context.Trips
                    .Include(t => t.RouteNavigation)
                        .ThenInclude(r => r.StartNavigation)
                    .Include(t => t.RouteNavigation)
                        .ThenInclude(r => r.StopNavigation)
                    .Include(t => t.TransportNavigation)
                    .Where(t => t.DepatureTime > DateTime.Now)
                    .OrderBy(t => t.DepatureTime)
                    .ToListAsync();

                if (availableTrips == null || !availableTrips.Any())
                {
                    ViewBag.Trips = new SelectList(new List<SelectListItem>(), "Id", "DisplayText");
                    ViewBag.Message = "Нет доступных рейсов для бронирования.";
                }
                else
                {
                    var tripSelectList = new List<SelectListItem>();
                    foreach (var trip in availableTrips)
                    {
                        var freeSeats = await GetFreeSeatsCount(trip.Id);
                        var displayText = $"{trip.RouteNavigation?.Name ?? "Неизвестный маршрут"} - " +
                                         $"{trip.DepatureTime:dd.MM.yyyy HH:mm} - " +
                                         $"{trip.TransportNavigation?.Model ?? "Неизвестный транспорт"} " +
                                         $"(Свободно мест: {freeSeats})";

                        tripSelectList.Add(new SelectListItem
                        {
                            Value = trip.Id.ToString(),
                            Text = displayText
                        });
                    }

                    ViewBag.Trips = new SelectList(tripSelectList, "Value", "Text");
                }

                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Create GET: {ex.Message}");
                ViewBag.Trips = new SelectList(new List<SelectListItem>(), "Value", "Text");
                ViewBag.Error = "Ошибка при загрузке рейсов: " + ex.Message;
                return View();
            }
        }

        // POST: Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create([Bind("Trip,SeatNumber")] Order order)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    ModelState.AddModelError("", "Пользователь не авторизован");
                    await LoadTripsForCreate(order.Trip);
                    return View(order);
                }

                order.User = currentUser.Id; 
                order.Stasus = "Ожидает подтверждения";   

                if (order.Trip <= 0)
                {
                    ModelState.AddModelError("Trip", "Пожалуйста, выберите рейс");
                    await LoadTripsForCreate(null);
                    return View(order);
                }

                var trip = await _context.Trips
                    .Include(t => t.RouteNavigation)
                    .Include(t => t.TransportNavigation)
                    .FirstOrDefaultAsync(t => t.Id == order.Trip);

                if (trip == null)
                {
                    ModelState.AddModelError("Trip", "Выбранный рейс не существует");
                    await LoadTripsForCreate(order.Trip);
                    return View(order);
                }

                if (trip.DepatureTime <= DateTime.Now)
                {
                    ModelState.AddModelError("Trip", "Нельзя забронировать билет на прошедший рейс");
                    await LoadTripsForCreate(order.Trip);
                    return View(order);
                }

                order.Price = trip.Price;

                if (order.SeatNumber <= 0)
                {
                    ModelState.AddModelError("SeatNumber", "Номер места должен быть положительным числом");
                    await LoadTripsForCreate(order.Trip);
                    return View(order);
                }

                if (trip.TransportNavigation != null && order.SeatNumber > trip.TransportNavigation.Capacity)
                {
                    ModelState.AddModelError("SeatNumber", $"Максимальный номер места - {trip.TransportNavigation.Capacity}");
                    await LoadTripsForCreate(order.Trip);
                    return View(order);
                }

                var existingOrder = await _context.Orders
                    .FirstOrDefaultAsync(o => o.Trip == order.Trip && o.SeatNumber == order.SeatNumber && o.Stasus != "Отменен");

                if (existingOrder != null)
                {
                    ModelState.AddModelError("SeatNumber", $"Место {order.SeatNumber} уже занято! Выберите другое место.");
                    await LoadTripsForCreate(order.Trip);
                    return View(order);
                }

                ModelState.Remove("UserNavigation");
                ModelState.Remove("TripNavigation");
                ModelState.Remove("Price");
                ModelState.Remove("Stasus");
                ModelState.Remove("User");

                if (ModelState.IsValid)
                {
                    Console.WriteLine("=== ПОПЫТКА СОХРАНЕНИЯ ЗАКАЗА ===");
                    Console.WriteLine($"Trip ID: {order.Trip}");
                    Console.WriteLine($"Seat Number: {order.SeatNumber}");
                    Console.WriteLine($"User ID: '{order.User}'");
                    Console.WriteLine($"Price: {order.Price}");
                    Console.WriteLine($"Status: '{order.Stasus}'");

                    var userExists = await _context.Users.AnyAsync(u => u.Id == order.User);
                    Console.WriteLine($"User exists in database: {userExists}");

                    if (!userExists)
                    {
                        Console.WriteLine($"User with ID '{order.User}' not found. Trying with UserName...");
                        order.User = currentUser.UserName;

                        var userExistsByName = await _context.Users.AnyAsync(u => u.UserName == order.User);
                        Console.WriteLine($"User exists by UserName: {userExistsByName}");

                        if (!userExistsByName)
                        {
                            ModelState.AddModelError("", $"Пользователь не найден в системе");
                            await LoadTripsForCreate(order.Trip);
                            return View(order);
                        }
                    }

                    var tripExists = await _context.Trips.AnyAsync(t => t.Id == order.Trip);
                    Console.WriteLine($"Trip exists in database: {tripExists}");

                    try
                    {
                        _context.Orders.Add(order);
                        Console.WriteLine($"Order added to context. State: {_context.Entry(order).State}");

                        await _context.SaveChangesAsync();
                        Console.WriteLine($"SaveChanges SUCCESS! Order ID: {order.Id}");

                        TempData["Message"] = $"✅ Заказ успешно создан! Номер заказа: {order.Id}, Сумма: {order.Price:C}";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (DbUpdateException dbEx)
                    {
                        Console.WriteLine($"DbUpdateException: {dbEx.Message}");

                        if (dbEx.InnerException != null)
                        {
                            Console.WriteLine($"Inner Exception: {dbEx.InnerException.Message}");

                            if (dbEx.InnerException.Message.Contains("FOREIGN KEY"))
                            {
                                ModelState.AddModelError("", "Ошибка связи с пользователем. Попробуйте выйти и войти снова.");

                                Console.WriteLine("Attempting to save without foreign key constraint...");

                                var sql = @"
                            INSERT INTO orders (trip, seatNumber, [user], price, stasus) 
                            VALUES (@p0, @p1, @p2, @p3, @p4)";

                                await _context.Database.ExecuteSqlRawAsync(sql,
                                    order.Trip,
                                    order.SeatNumber,
                                    order.User,
                                    order.Price,
                                    order.Stasus);

                                Console.WriteLine("Direct SQL insert SUCCESS!");
                                TempData["Message"] = $"✅ Заказ успешно создан! Сумма: {order.Price:C}";
                                return RedirectToAction(nameof(Index));
                            }
                        }

                        ModelState.AddModelError("", $"Ошибка базы данных: {dbEx.InnerException?.Message ?? dbEx.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"General Exception: {ex.Message}");
                        ModelState.AddModelError("", $"Ошибка: {ex.Message}");
                    }
                }

                await LoadTripsForCreate(order.Trip);

                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Model Error: {error.ErrorMessage}");
                }

                return View(order);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UNHANDLED EXCEPTION: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                ModelState.AddModelError("", $"Произошла непредвиденная ошибка: {ex.Message}");
                await LoadTripsForCreate(order?.Trip);
                return View(order ?? new Order());
            }
        }

        private async Task LoadTripsForCreate(int? selectedTrip = null)
        {
            try
            {
                var availableTrips = await _context.Trips
                    .Include(t => t.RouteNavigation)
                        .ThenInclude(r => r.StartNavigation)
                    .Include(t => t.RouteNavigation)
                        .ThenInclude(r => r.StopNavigation)
                    .Include(t => t.TransportNavigation)
                    .Where(t => t.DepatureTime > DateTime.Now)
                    .OrderBy(t => t.DepatureTime)
                    .ToListAsync();

                if (availableTrips == null || !availableTrips.Any())
                {
                    ViewBag.Trips = new SelectList(new List<SelectListItem>(), "Value", "Text");
                    ViewBag.Message = "Нет доступных рейсов для бронирования.";
                    return;
                }

                var tripSelectList = new List<SelectListItem>();
                foreach (var trip in availableTrips)
                {
                    var freeSeats = await GetFreeSeatsCount(trip.Id);
                    var routeName = trip.RouteNavigation?.Name ?? "Неизвестный маршрут";
                    var transportModel = trip.TransportNavigation?.Model ?? "Неизвестный транспорт";
                    var depatureTime = trip.DepatureTime.ToString("dd.MM.yyyy HH:mm");

                    var displayText = $"{routeName} - {depatureTime} - {transportModel} (Свободно: {freeSeats})";

                    tripSelectList.Add(new SelectListItem
                    {
                        Value = trip.Id.ToString(),
                        Text = displayText
                    });
                }

                ViewBag.Trips = new SelectList(tripSelectList, "Value", "Text", selectedTrip?.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in LoadTripsForCreate: {ex.Message}");
                ViewBag.Trips = new SelectList(new List<SelectListItem>(), "Value", "Text");
                ViewBag.Error = "Ошибка при загрузке списка рейсов";
            }
        }

        private async Task<int> GetFreeSeatsCount(int tripId)
        {
            try
            {
                var trip = await _context.Trips
                    .Include(t => t.TransportNavigation)
                    .FirstOrDefaultAsync(t => t.Id == tripId);

                if (trip?.TransportNavigation == null) return 0;

                var occupiedSeats = await _context.Orders
                    .Where(o => o.Trip == tripId && o.Stasus != "Отменен")
                    .CountAsync();

                return trip.TransportNavigation.Capacity - occupiedSeats;
            }
            catch
            {
                return 0;
            }
        }

        // GET: Orders/GetAvailableSeats (для AJAX)
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAvailableSeats(int tripId)
        {
            try
            {
                var trip = await _context.Trips
                    .Include(t => t.TransportNavigation)
                    .FirstOrDefaultAsync(t => t.Id == tripId);

                if (trip?.TransportNavigation == null)
                {
                    return Json(new { error = "Рейс не найден" });
                }

                var totalSeats = trip.TransportNavigation.Capacity;

                var occupiedSeats = await _context.Orders
                    .Where(o => o.Trip == tripId && o.Stasus != "Отменен")
                    .Select(o => o.SeatNumber)
                    .ToListAsync();

                var availableSeats = Enumerable.Range(1, totalSeats)
                    .Where(s => !occupiedSeats.Contains(s))
                    .ToList();

                return Json(new
                {
                    success = true,
                    totalSeats = totalSeats,
                    occupiedSeats = occupiedSeats,
                    availableSeats = availableSeats,
                    freeSeatsCount = availableSeats.Count
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
        // GET: Orders/Edit/5
        [Authorize(Roles = "Admin,Dispatcher")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders
                .Include(o => o.TripNavigation)
                .ThenInclude(t => t.RouteNavigation)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            // Загружаем доступные рейсы
            var trips = await _context.Trips
                .Include(t => t.RouteNavigation)
                .Where(t => t.DepatureTime > DateTime.Now)
                .ToListAsync();

            ViewBag.Trips = new SelectList(trips, "Id", "RouteNavigation.Name", order.Trip);

            // Для отладки - выводим значение User
            Console.WriteLine($"Editing order {id}: User = {order.User}");

            return View(order);
        }

        // POST: Orders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Dispatcher")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Trip,SeatNumber,Price,Stasus")] Order order)
        {
            if (id != order.Id) return NotFound();

            var existingOrder = await _context.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == id);

            if (existingOrder == null) return NotFound();


            order.User = existingOrder.User;


            ModelState.Remove("UserNavigation");
            ModelState.Remove("TripNavigation");
            ModelState.Remove("User");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Orders.Update(order);
                    await _context.SaveChangesAsync();
                    TempData["Message"] = "Заказ успешно обновлен!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(order.Id)) return NotFound();
                    else throw;
                }
                catch (DbUpdateException dbEx)
                {
                    Console.WriteLine($"DB Error: {dbEx.InnerException?.Message ?? dbEx.Message}");
                    ModelState.AddModelError("", $"Ошибка при обновлении: {dbEx.InnerException?.Message ?? dbEx.Message}");
                }
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

        // GET: Orders/Payment/5
        [Authorize]
        public async Task<IActionResult> Payment(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders
                .Include(o => o.TripNavigation)
                    .ThenInclude(t => t.RouteNavigation)
                .Include(o => o.UserNavigation)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var userRole = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;

            if (userRole == "User" && order.User != currentUser.Id)
            {
                return Forbid();
            }

            if (order.Stasus != "Ожидает подтверждения")
            {
                TempData["Error"] = "Оплата возможна только для заказов в статусе 'Ожидает подтверждения'";
                return RedirectToAction(nameof(Index));
            }

            var model = new PaymentViewModel
            {
                OrderId = order.Id,
                Amount = order.Price,
                OrderDetails = $"{order.TripNavigation?.RouteNavigation?.Name} - Место {order.SeatNumber}",
                PassengerName = order.UserNavigation?.UserName ?? order.User
            };

            return View(model);
        }

        // POST: Orders/Payment
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Payment(PaymentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var order = await _context.Orders
                .Include(o => o.TripNavigation)
                .FirstOrDefaultAsync(o => o.Id == model.OrderId);

            if (order == null)
            {
                TempData["Error"] = "Заказ не найден";
                return RedirectToAction(nameof(Index));
            }

            if (order.Stasus != "Ожидает подтверждения")
            {
                TempData["Error"] = "Этот заказ уже не может быть оплачен";
                return RedirectToAction(nameof(Index));
            }

            bool paymentSuccess = SimulatePayment(model);

            if (paymentSuccess)
            {
                order.Stasus = "Подтвержден";
                _context.Update(order);
                await _context.SaveChangesAsync();

                var ticketNumber = GenerateTicketNumber(order);

                TempData["Message"] = $"✅ Оплата успешно произведена! Номер билета: {ticketNumber}";

                return RedirectToAction(nameof(Ticket), new { id = order.Id });
            }
            else
            {
                TempData["Error"] = "Ошибка при обработке платежа. Попробуйте еще раз.";
                return RedirectToAction(nameof(Payment), new { id = order.Id });
            }
        }

        // GET: Orders/Ticket/5
        [Authorize]
        public async Task<IActionResult> Ticket(int? id)
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
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var userRole = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;

            if (userRole == "User" && order.User != currentUser.Id)
            {
                return Forbid();
            }

            var ticket = new TicketViewModel
            {
                OrderId = order.Id,
                TicketNumber = GenerateTicketNumber(order),
                RouteName = order.TripNavigation?.RouteNavigation?.Name ?? "Неизвестный маршрут",
                StartCity = order.TripNavigation?.RouteNavigation?.StartNavigation?.Name ?? "Не указано",
                StopCity = order.TripNavigation?.RouteNavigation?.StopNavigation?.Name ?? "Не указано",
                DepartureTime = order.TripNavigation?.DepatureTime ?? DateTime.Now,
                ArrivalTime = order.TripNavigation?.ArrivalTime ?? DateTime.Now,
                SeatNumber = order.SeatNumber,
                Price = order.Price,
                PassengerName = order.UserNavigation?.UserName ?? order.User,
                TransportModel = order.TripNavigation?.TransportNavigation?.Model ?? "Не указан",
                CarNumber = order.TripNavigation?.TransportNavigation?.CarNumber ?? "Не указан"
            };

            return View(ticket);
        }

        private bool SimulatePayment(PaymentViewModel model)
        {
            if (string.IsNullOrEmpty(model.CardNumber) || model.CardNumber.Length < 16)
                return false;

            if (string.IsNullOrEmpty(model.CardHolder) || model.CardHolder.Length < 3)
                return false;

            if (string.IsNullOrEmpty(model.ExpiryDate) || model.ExpiryDate.Length != 5)
                return false;

            if (string.IsNullOrEmpty(model.CVV) || model.CVV.Length < 3)
                return false;

            var random = new Random();
            return random.Next(1, 101) <= 90;
        }

        private string GenerateTicketNumber(Order order)
        {
            var date = DateTime.Now.ToString("yyyyMMdd");
            var random = new Random().Next(10000, 99999);
            return $"TL-{date}-{random}-{order.Id}";
        }
    }
}