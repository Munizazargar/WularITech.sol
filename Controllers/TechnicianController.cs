using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WularItech_solutions.Interfaces;
using WularItech_solutions.Models;

namespace WularItech_solutions.Controllers
{
    public class TechnicianController : Controller
    {
        private readonly SqlDbContext _db;
        private readonly ITokenService _tokenService;

        public TechnicianController(SqlDbContext db, ITokenService tokenService)
        {
            _db = db;
            _tokenService = tokenService;
        }

        private Guid? GetTechnicianId()
        {
            var token = Request.Cookies["tech_jwt"];
            if (string.IsNullOrEmpty(token)) return null;
            return _tokenService.GetTechnicianId(token);
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (GetTechnicianId() != null) return RedirectToAction("Dashboard");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string phone, string password)
        {
            var tech = await _db.Technicians
                .FirstOrDefaultAsync(t => t.Phone == phone && t.IsActive == true);

            if (tech == null || !BCrypt.Net.BCrypt.Verify(password, tech.PasswordHash))
            {
                ViewBag.Error = "Invalid phone or password.";
                return View();
            }

            var token = _tokenService.CreateTechnicianToken(tech);
            Response.Cookies.Append("tech_jwt", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddHours(8)
            });

            return RedirectToAction("Dashboard");
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var techId = GetTechnicianId();
            if (techId == null) return RedirectToAction("Login");

            var tech = await _db.Technicians.FindAsync(techId);
            var bookings = await _db.Bookings
                .Where(b => b.TechnicianId == techId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            ViewBag.TechnicianName = tech?.FullName;
            return View(bookings);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(Guid id, string status)
        {
            var techId = GetTechnicianId();
            if (techId == null) return RedirectToAction("Login");

            var booking = await _db.Bookings
                .FirstOrDefaultAsync(b => b.BookingId == id && b.TechnicianId == techId);

            if (booking == null) return NotFound();

            booking.Status = status;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Status updated!";
            return RedirectToAction("Dashboard");
        }

        public IActionResult Logout()
        {
            Response.Cookies.Delete("tech_jwt");
            return RedirectToAction("Login");
        }
    }
}