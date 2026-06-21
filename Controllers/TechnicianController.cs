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

    var bookingIds = bookings.Select(b => b.BookingId).ToList();
    var myReviews = await _db.Reviews
        .Where(r => bookingIds.Contains(r.BookingId))
        .OrderByDescending(r => r.CreatedAt)
        .ToListAsync();

    ViewBag.TechnicianName = tech?.FullName;
    ViewBag.Reviews = myReviews;
    ViewBag.AverageRating = myReviews.Any() ? Math.Round(myReviews.Average(r => r.Rating), 1) : (double?)null;

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