using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WularItech_solutions.Interfaces;
using WularItech_solutions.Models;

namespace WularItech_solutions.Controllers
{
    public class AdminController : Controller
    {
        private readonly SqlDbContext _db;
        private readonly ITokenService _tokenService;
        private readonly ICloudinaryService _cloudinaryService;

        private readonly IEmailService _emailService;

        public AdminController(SqlDbContext db, ITokenService tokenService, ICloudinaryService cloudinaryService, IEmailService emailService)
        {
            _db = db;
            _tokenService = tokenService;
            _cloudinaryService = cloudinaryService;
            _emailService = emailService;
        }

        private bool IsAdmin()
        {
            var token = Request.Cookies["jwt"];
            return !string.IsNullOrEmpty(token) && _tokenService.IsAdmin(token);
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            ViewBag.TotalProducts = await _db.Products.CountAsync();
            ViewBag.TotalUsers = await _db.Users.CountAsync();
            ViewBag.TotalBookings = await _db.Bookings.CountAsync();
            ViewBag.PendingBookings = await _db.Bookings.CountAsync(b => b.Status == "Pending");
            ViewBag.TotalContacts = await _db.Contacts.CountAsync();
            ViewBag.RecentBookings = await _db.Bookings
                .OrderByDescending(b => b.CreatedAt)
                .Take(5)
                .ToListAsync();

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Products()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var products = await _db.Products.ToListAsync();
            return View(products);
        }

        [HttpGet]
        public IActionResult CreateProduct()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct(Product model, IFormFile image)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            ModelState.Remove("ProductImage");
            if (!ModelState.IsValid) return View(model);

            if (await _db.Products.AnyAsync(p => p.ProductName == model.ProductName))
            {
                ViewBag.Message = "Product already exists.";
                return View(model);
            }

            if (image != null && image.Length > 0)
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                model.ProductImage = await _cloudinaryService.UploadImageAsync(image, "products").WaitAsync(cts.Token);
            }

            _db.Products.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Product created successfully!";
            return RedirectToAction("Products");
        }

        [HttpGet]
        public async Task<IActionResult> EditProduct(Guid id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var product = await _db.Products.FindAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> EditProduct(Product model, IFormFile image)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var existing = await _db.Products.FindAsync(model.ProductId);
            if (existing == null) return NotFound();

            existing.ProductName = model.ProductName;
            existing.ProductDescription = model.ProductDescription;
            existing.ProductPrice = model.ProductPrice;
            existing.ProductStock = model.ProductStock;

            if (image != null && image.Length > 0)
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                existing.ProductImage = await _cloudinaryService.UploadImageAsync(image, "products").WaitAsync(cts.Token);
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Product updated successfully!";
            return RedirectToAction("Products");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteProduct(Guid id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var product = await _db.Products.FindAsync(id);
            if (product == null) return NotFound();
            _db.Products.Remove(product);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Product deleted.";
            return RedirectToAction("Products");
        }

        [HttpGet]
        public async Task<IActionResult> Bookings()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var bookings = await _db.Bookings.OrderByDescending(b => b.CreatedAt).ToListAsync();
            return View(bookings);
        }


        [HttpPost]
        public async Task<IActionResult> UpdateBookingStatus(Guid id, string status)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var booking = await _db.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            booking.Status = status;
            await _db.SaveChangesAsync();

            Console.WriteLine($"DEBUG → Email: '{booking.CustomerEmail}' | Name: '{booking.CustomerName}' | Service: '{booking.ServiceType}' | Address: '{booking.Address}' | Date: '{booking.PreferredDate}' | Notes: '{booking.Notes}'");

            _ = Task.Run(async () =>
            {
                try
                {
                    var statusColor = status switch
                    {
                        "Confirmed" => "#065f46",
                        "InProgress" => "#1e40af",
                        "Completed" => "#5b21b6",
                        "Cancelled" => "#991b1b",
                        _ => "#92400E"
                    };

                    var statusBg = status switch
                    {
                        "Confirmed" => "#d1fae5",
                        "InProgress" => "#dbeafe",
                        "Completed" => "#ede9fe",
                        "Cancelled" => "#fee2e2",
                        _ => "#FEF3C7"
                    };

                    var statusEmoji = status switch
                    {
                        "Confirmed" => "✅",
                        "InProgress" => "🔧",
                        "Completed" => "🎉",
                        "Cancelled" => "❌",
                        _ => "🔔"
                    };

                    var statusMessage = status switch
                    {
                        "Confirmed" => "Great news! Your booking has been confirmed. Our technician will arrive on the scheduled date.",
                        "InProgress" => "Our technician is currently on the way and working on your service request.",
                        "Completed" => "Your service has been completed successfully. Thank you for choosing WularTech Solutions!",
                        "Cancelled" => "Unfortunately your booking has been cancelled. Please contact us to reschedule.",
                        _ => "Your booking status has been updated."
                    };

                    // Safe values — no nulls
                    var customerName = booking.CustomerName ?? "Customer";
                    var serviceType = booking.ServiceType ?? "Service";
                    var address = booking.Address ?? "N/A";
                    var notes = booking.Notes ?? "";
                    var preferredDate = booking.PreferredDate != default
                                        ? booking.PreferredDate.ToString("dd MMM yyyy")
                                        : "To be confirmed";

                    var subject = $"Booking Update: {status} {statusEmoji} - WularTech Solutions";

                    var body = $@"
                <div style='font-family: Inter, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <div style='background: #1A1A2E; padding: 24px; border-radius: 12px 12px 0 0;'>
                        <h1 style='color: #E07B39; margin: 0; font-size: 24px;'>WularTech Solutions</h1>
                        <p style='color: rgba(255,255,255,0.7); margin: 4px 0 0;'>Security &amp; Electrical Services</p>
                    </div>
                    <div style='background: #ffffff; padding: 32px; border: 1px solid #E5E7EB;'>
                        <h2 style='color: #111827; margin-top: 0;'>Booking {status}! {statusEmoji}</h2>
                        <p style='color: #6B7280;'>Dear <strong>{customerName}</strong>, {statusMessage}</p>
                        <div style='background: #F9FAFB; border-radius: 8px; padding: 20px; margin: 24px 0;'>
                            <table style='width: 100%; border-collapse: collapse;'>
                                <tr><td style='padding: 8px 0; color: #6B7280; width: 140px;'>Service</td><td style='padding: 8px 0; color: #111827; font-weight: 600;'>{serviceType}</td></tr>
                                <tr><td style='padding: 8px 0; color: #6B7280;'>Date</td><td style='padding: 8px 0; color: #111827; font-weight: 600;'>{preferredDate}</td></tr>
                                <tr><td style='padding: 8px 0; color: #6B7280;'>Address</td><td style='padding: 8px 0; color: #111827; font-weight: 600;'>{address}</td></tr>
                                <tr>
                                    <td style='padding: 8px 0; color: #6B7280;'>Status</td>
                                    <td style='padding: 8px 0;'>
                                        <span style='background: {statusBg}; color: {statusColor}; padding: 4px 10px; border-radius: 50px; font-size: 12px; font-weight: 600;'>
                                            {status} {statusEmoji}
                                        </span>
                                    </td>
                                </tr>
                            </table>
                        </div>
                        <p style='color: #6B7280;'>For any queries, contact us:</p>
                        <p style='color: #6B7280;'>📞 <a href='https://wa.me/918825048116' style='color: #E07B39;'>+91 88250 48116</a></p>
                        <p style='color: #6B7280;'>📧 <a href='mailto:hyatt.wular@gmail.com' style='color: #E07B39;'>hyatt.wular@gmail.com</a></p>
                    </div>
                    <div style='background: #F9FAFB; padding: 16px; text-align: center; border-radius: 0 0 12px 12px; border: 1px solid #E5E7EB; border-top: none;'>
                        <p style='color: #9CA3AF; font-size: 13px; margin: 0;'>© 2026 WularTech Solutions. Bemina, Srinagar.</p>
                    </div>
                </div>";

                    await _emailService.SendEmailAsync(booking.CustomerEmail, subject, body);
                    Console.WriteLine($"Email sent to {booking.CustomerEmail} — {status}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Email failed: " + ex.Message);
                    Console.WriteLine("Stack: " + ex.StackTrace);
                }
            });

            TempData["Success"] = $"Booking marked as {status}.";
            return RedirectToAction("Bookings");
        }


        [HttpPost]
        public async Task<IActionResult> DeleteBooking(Guid id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var booking = await _db.Bookings.FindAsync(id);
            if (booking == null) return NotFound();
            _db.Bookings.Remove(booking);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Booking deleted.";
            return RedirectToAction("Bookings");
        }

        [HttpGet]
        public async Task<IActionResult> Contacts()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var contacts = await _db.Contacts.OrderByDescending(c => c.CreatedAt).ToListAsync();
            return View(contacts);
        }


        [HttpPost]
        public async Task<IActionResult> DeleteContact(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var contact = await _db.Contacts.FindAsync(id);
            if (contact == null) return NotFound();
            _db.Contacts.Remove(contact);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Message deleted.";
            return RedirectToAction("Contacts");
        }

        [HttpGet]
        public async Task<IActionResult> Users()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var users = await _db.Users.ToListAsync();
            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleAdmin(Guid id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();
            user.IsAdmin = !user.IsAdmin;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"{user.Username} admin status updated.";
            return RedirectToAction("Users");
        }

        // ─── TECHNICIANS ────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Technicians()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var technicians = await _db.Technicians.OrderByDescending(t => t.CreatedAt).ToListAsync();
            return View(technicians);
        }

        [HttpGet]
        public IActionResult AddTechnician()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddTechnician(Technician model)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            if (!ModelState.IsValid) return View(model);

            _db.Technicians.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Technician added successfully!";
            return RedirectToAction("Technicians");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleTechnician(Guid id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var tech = await _db.Technicians.FindAsync(id);
            if (tech == null) return NotFound();
            tech.IsActive = !tech.IsActive;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"{tech.FullName} status updated.";
            return RedirectToAction("Technicians");
        }

        // ─── ASSIGN TECHNICIAN TO BOOKING ───────────────

        [HttpPost]
        public async Task<IActionResult> AssignTechnician(Guid bookingId, Guid technicianId)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var booking = await _db.Bookings.FindAsync(bookingId);
            if (booking == null) return NotFound();

            booking.TechnicianId = technicianId;
            await _db.SaveChangesAsync();

            TempData["Success"] = "Technician assigned!";
            return RedirectToAction("Bookings");
        }
    }
}