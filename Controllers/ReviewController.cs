using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WularItech_solutions.Interfaces;
using WularItech_solutions.Models;
using WularItech_solutions.ViewModels;

namespace WularItech_solutions.Controllers
{
    public class ReviewController : Controller
    {
        private readonly SqlDbContext dbContext;
        private readonly ITokenService tokenService;

        public ReviewController(SqlDbContext dbContext, ITokenService tokenService)
        {
            this.dbContext = dbContext;
            this.tokenService = tokenService;
        }

        private async Task<User?> GetCurrentUserAsync()
        {
            var token = Request.Cookies["jwt"];
            if (string.IsNullOrEmpty(token)) return null;

            Guid userId;
            try
            {
                userId = tokenService.VerifyTokenAndGetId(token);
            }
            catch
            {
                return null;
            }

            return await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        }

        // Shows the review form for a specific completed booking the user owns.
        [HttpGet]
        public async Task<ActionResult> Create(Guid bookingId)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var booking = await dbContext.Bookings.FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null || booking.CustomerEmail != user.Email)
            {
                ViewBag.errorMessage = "Booking not found.";
                return View("ReviewError");
            }

            if (booking.Status != "Completed")
            {
                ViewBag.errorMessage = "You can only review completed bookings.";
                return View("ReviewError");
            }

            var existingReview = await dbContext.Reviews.FirstOrDefaultAsync(r => r.BookingId == bookingId);
            if (existingReview != null)
            {
                ViewBag.errorMessage = "You've already reviewed this booking.";
                return View("ReviewError");
            }

            var model = new CreateReviewViewModel { BookingId = bookingId, Comment = "" };
            ViewBag.ServiceType = booking.ServiceType;
            return View(model);
        }

        [HttpPost]
        public async Task<ActionResult> Create(CreateReviewViewModel model)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var booking = await dbContext.Bookings.FirstOrDefaultAsync(b => b.BookingId == model.BookingId);

            if (booking == null || booking.CustomerEmail != user.Email)
            {
                ViewBag.errorMessage = "Booking not found.";
                return View("ReviewError");
            }

            if (booking.Status != "Completed")
            {
                ViewBag.errorMessage = "You can only review completed bookings.";
                return View("ReviewError");
            }

            var existingReview = await dbContext.Reviews.FirstOrDefaultAsync(r => r.BookingId == model.BookingId);
            if (existingReview != null)
            {
                ViewBag.errorMessage = "You've already reviewed this booking.";
                return View("ReviewError");
            }

            var review = new Review
            {
                BookingId = booking.BookingId,
                UserId = user.Id,
                CustomerName = booking.CustomerName,
                ServiceType = booking.ServiceType,
                Rating = model.Rating,
                Comment = model.Comment
            };

            await dbContext.Reviews.AddAsync(review);
            await dbContext.SaveChangesAsync();

            ViewBag.message = "Thank you for your review!";
            return View("ReviewThankYou");
        }

        // Public testimonials page — anyone can view, no login required.
        [HttpGet]
        public async Task<ActionResult> Index()
        {
            var reviews = await dbContext.Reviews
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(reviews);
        }

        [HttpGet]
public async Task<ActionResult> Edit(Guid bookingId)
{
    var user = await GetCurrentUserAsync();
    if (user == null)
    {
        return RedirectToAction("Login", "Account");
    }

    var review = await dbContext.Reviews.FirstOrDefaultAsync(r => r.BookingId == bookingId && r.UserId == user.Id);

    if (review == null)
    {
        ViewBag.errorMessage = "Review not found.";
        return View("ReviewError");
    }

    var model = new CreateReviewViewModel
    {
        BookingId = review.BookingId,
        Rating = review.Rating,
        Comment = review.Comment
    };

    ViewBag.ServiceType = review.ServiceType;
    return View("Create", model);
}

[HttpPost]
public async Task<ActionResult> Edit(CreateReviewViewModel model)
{
    var user = await GetCurrentUserAsync();
    if (user == null)
    {
        return RedirectToAction("Login", "Account");
    }

    if (!ModelState.IsValid)
    {
        return View("Create", model);
    }

    var review = await dbContext.Reviews.FirstOrDefaultAsync(r => r.BookingId == model.BookingId && r.UserId == user.Id);

    if (review == null)
    {
        ViewBag.errorMessage = "Review not found.";
        return View("ReviewError");
    }

    review.Rating = model.Rating;
    review.Comment = model.Comment;
    await dbContext.SaveChangesAsync();

    ViewBag.message = "Your review has been updated.";
    return View("ReviewThankYou");
}
    }
}