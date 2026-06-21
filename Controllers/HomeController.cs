using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WularItech_solutions.Models;

namespace WularItech_solutions.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly SqlDbContext _db;

    public HomeController(ILogger<HomeController> logger, SqlDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        // Top rated technician(s): average rating across their completed jobs,
        // only counted if they have at least 3 reviews (avoids one lucky 5-star
        // review crowning someone "top rated" off a single job).
        var topTechnicians = await _db.Technicians
            .Where(t => t.IsActive)
            .Select(t => new
            {
                Technician = t,
                ReviewCount = _db.Reviews
                    .Count(r => _db.Bookings.Any(b => b.BookingId == r.BookingId && b.TechnicianId == t.TechnicianId)),
                AverageRating = _db.Reviews
                    .Where(r => _db.Bookings.Any(b => b.BookingId == r.BookingId && b.TechnicianId == t.TechnicianId))
                    .Average(r => (double?)r.Rating)
            })
            .Where(x => x.ReviewCount >= 3)
            .OrderByDescending(x => x.AverageRating)
            .Take(3)
            .ToListAsync();

        ViewBag.TopTechnicians = topTechnicians.Select(x => new
        {
            x.Technician.FullName,
            x.Technician.Skill,
            x.Technician.Area,
            AverageRating = Math.Round(x.AverageRating ?? 0, 1),
            x.ReviewCount
        }).ToList();

        return View();
    }

    [HttpGet]
    public IActionResult Pricing()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}