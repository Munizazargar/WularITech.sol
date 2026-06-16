using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WularItech_solutions.Interfaces;
using WularItech_solutions.Models;

namespace WularItech_solutions.Controllers
{
    public class BookingController : Controller
    {
        private readonly SqlDbContext _db;
        private readonly IEmailService _emailService;

        public BookingController(SqlDbContext db, IEmailService emailService)
        {
            _db = db;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Create()
        {
            var token = Request.Cookies["jwt"];
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Booking model)
        {
            var token = Request.Cookies["jwt"];
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please fill all required fields.";
                return View(model);
            }

            model.BookingId = Guid.NewGuid();
            model.CreatedAt = DateTime.UtcNow;
            model.PreferredDate = DateTime.SpecifyKind(model.PreferredDate, DateTimeKind.Utc);
            model.Status = "Pending";

            try
            {
                _db.Bookings.Add(model);
                await _db.SaveChangesAsync();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                TempData["Success"] = "Booking already submitted! We'll contact you shortly.";
                return RedirectToAction("Create");
            }

            // Send confirmation email to customer
            try
            {
                var customerSubject = "Booking Confirmed - WularTech Solutions";
                var customerBody = $@"
                    <div style='font-family: Inter, sans-serif; max-width: 600px; margin: 0 auto;'>
                        <div style='background: #1A1A2E; padding: 24px; border-radius: 12px 12px 0 0;'>
                            <h1 style='color: #E07B39; margin: 0; font-size: 24px;'>WularTech Solutions</h1>
                            <p style='color: rgba(255,255,255,0.7); margin: 4px 0 0;'>Security & Electrical Services</p>
                        </div>
                        <div style='background: #ffffff; padding: 32px; border: 1px solid #E5E7EB;'>
                            <h2 style='color: #111827; margin-top: 0;'>Booking Received! ✅</h2>
                            <p style='color: #6B7280;'>Dear <strong>{model.CustomerName}</strong>, your service booking has been received. We'll contact you shortly to confirm.</p>
                            <div style='background: #F9FAFB; border-radius: 8px; padding: 20px; margin: 24px 0;'>
                                <h3 style='color: #111827; margin-top: 0; font-size: 16px;'>Booking Details</h3>
                                <table style='width: 100%; border-collapse: collapse;'>
                                    <tr><td style='padding: 8px 0; color: #6B7280; width: 140px;'>Service</td><td style='padding: 8px 0; color: #111827; font-weight: 600;'>{model.ServiceType}</td></tr>
                                    <tr><td style='padding: 8px 0; color: #6B7280;'>Preferred Date</td><td style='padding: 8px 0; color: #111827; font-weight: 600;'>{model.PreferredDate:dd MMM yyyy}</td></tr>
                                    <tr><td style='padding: 8px 0; color: #6B7280;'>Address</td><td style='padding: 8px 0; color: #111827; font-weight: 600;'>{model.Address}</td></tr>
                                    <tr><td style='padding: 8px 0; color: #6B7280;'>Status</td><td style='padding: 8px 0;'><span style='background: #FEF3C7; color: #92400E; padding: 4px 10px; border-radius: 50px; font-size: 12px; font-weight: 600;'>Pending</span></td></tr>
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
                using var cts1 = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                await _emailService.SendEmailAsync(model.CustomerEmail, customerSubject, customerBody).WaitAsync(cts1.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Customer email failed: " + ex.Message);
                Console.WriteLine("Customer email inner: " + ex.InnerException?.Message);
                Console.WriteLine("Customer email inner2: " + ex.InnerException?.InnerException?.Message);
            }

            // Send notification email to admin
            try
            {
                var adminSubject = $"New Booking - {model.ServiceType} - {model.CustomerName}";
                var adminBody = $@"
                    <div style='font-family: Inter, sans-serif; max-width: 600px; margin: 0 auto;'>
                        <div style='background: #1A1A2E; padding: 24px; border-radius: 12px 12px 0 0;'>
                            <h1 style='color: #E07B39; margin: 0; font-size: 24px;'>New Booking Alert 🔔</h1>
                            <p style='color: rgba(255,255,255,0.7); margin: 4px 0 0;'>WularTech Admin Panel</p>
                        </div>
                        <div style='background: #ffffff; padding: 32px; border: 1px solid #E5E7EB;'>
                            <h2 style='color: #111827; margin-top: 0;'>New Service Booking Received</h2>
                            <div style='background: #F9FAFB; border-radius: 8px; padding: 20px; margin: 24px 0;'>
                                <table style='width: 100%; border-collapse: collapse;'>
                                    <tr><td style='padding: 8px 0; color: #6B7280; width: 140px;'>Customer</td><td style='padding: 8px 0; color: #111827; font-weight: 600;'>{model.CustomerName}</td></tr>
                                    <tr><td style='padding: 8px 0; color: #6B7280;'>Email</td><td style='padding: 8px 0; color: #111827; font-weight: 600;'>{model.CustomerEmail}</td></tr>
                                    <tr><td style='padding: 8px 0; color: #6B7280;'>Phone</td><td style='padding: 8px 0; color: #111827; font-weight: 600;'>{model.CustomerPhone}</td></tr>
                                    <tr><td style='padding: 8px 0; color: #6B7280;'>Service</td><td style='padding: 8px 0; color: #111827; font-weight: 600;'>{model.ServiceType}</td></tr>
                                    <tr><td style='padding: 8px 0; color: #6B7280;'>Date</td><td style='padding: 8px 0; color: #111827; font-weight: 600;'>{model.PreferredDate:dd MMM yyyy}</td></tr>
                                    <tr><td style='padding: 8px 0; color: #6B7280;'>Address</td><td style='padding: 8px 0; color: #111827; font-weight: 600;'>{model.Address}</td></tr>
                                    <tr><td style='padding: 8px 0; color: #6B7280;'>Notes</td><td style='padding: 8px 0; color: #111827;'>{(string.IsNullOrEmpty(model.Notes) ? "None" : model.Notes)}</td></tr>
                                </table>
                            </div>
                            <a href='https://wularitech-sol.onrender.com/Admin/Bookings' 
                               style='display: inline-block; background: #E07B39; color: #fff; padding: 12px 24px; border-radius: 8px; text-decoration: none; font-weight: 600;'>
                                View in Admin Dashboard →
                            </a>
                        </div>
                        <div style='background: #F9FAFB; padding: 16px; text-align: center; border-radius: 0 0 12px 12px; border: 1px solid #E5E7EB; border-top: none;'>
                            <p style='color: #9CA3AF; font-size: 13px; margin: 0;'>© 2026 WularTech Solutions</p>
                        </div>
                    </div>";
                using var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                await _emailService.SendEmailAsync("munizahzargar.iimun@gmail.com", adminSubject, adminBody).WaitAsync(cts2.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Admin email failed: " + ex.Message);
                Console.WriteLine("Admin email inner: " + ex.InnerException?.Message);
                Console.WriteLine("Admin email inner2: " + ex.InnerException?.InnerException?.Message);
            }

            TempData["Success"] = "Booking submitted! Check your email for confirmation.";
            return RedirectToAction("Create");
        }
    
[HttpGet]
public IActionResult Track()
{
    return View();
}

[HttpPost]
public async Task<IActionResult> Track(string search)
{
    if (string.IsNullOrWhiteSpace(search))
    {
        TempData["Error"] = "Please enter your email or phone number.";
        return View();
    }

    var bookings = await _db.Bookings
        .Where(b => b.CustomerEmail == search.Trim() || b.CustomerPhone == search.Trim())
        .OrderByDescending(b => b.CreatedAt)
        .ToListAsync();

    if (!bookings.Any())
    {
        TempData["Error"] = "No bookings found for that email or phone number.";
        return View();
    }

    return View(bookings);
}
    }}