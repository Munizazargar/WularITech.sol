using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using WularItech_solutions.Interfaces;
using WularItech_solutions.Models;
using WularItech_solutions.ViewModels;

namespace WularItech_solutions.Controllers
{
    public class AccountController : Controller
    {
        private readonly SqlDbContext dbContext;
        private readonly ITokenService tokenService;
        public AccountController(SqlDbContext dbContext, ITokenService tokenService)
        {
            this.dbContext = dbContext;
            this.tokenService = tokenService;
        }

        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }
        [HttpPost]
        public async Task<ActionResult> Register(User model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.message = "All credentials are reuired";
                return View();
            }
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user != null)
            {
                ViewBag.errorMessage = "User Already exists";
                return View();
            }
            model.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);
            await dbContext.Users.AddAsync(model);
            await dbContext.SaveChangesAsync();
            return RedirectToAction("Login");
        }
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public async Task<ActionResult> Login(Login model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.message = "All credentials are reuired";
                return View();
            }

            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user == null)
            {
                ViewBag.errorMessage = "user not found";
                return View(model);
            }
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(
            model.Password,
            user.Password);

            if (!isPasswordValid)
            {
                ViewBag.errorMessage = "Invalid email or password";
                return View(model);
            }
            var token = tokenService.CreateToken(user);

            Response.Cookies.Append("jwt", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = false, // set true when using https
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddHours(2)
            });


            return RedirectToAction("Index", "Home");

        }
        [HttpPost]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("jwt");
            return RedirectToAction("Login", "Account");
        }

    }
}