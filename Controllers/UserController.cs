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
        private readonly IEmailService emailService;
        public AccountController(SqlDbContext dbContext, ITokenService tokenService,IEmailService emailService  )
        {
            this.dbContext = dbContext;
            this.tokenService = tokenService;
            this.emailService = emailService;
        }

        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }
        [HttpPost]
public async Task<ActionResult> Register(RegisterViewModel model)
{
    if (!ModelState.IsValid)
    {
        ViewBag.message = "All credentials are required";
        return View();
    }

    var existingUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
    if (existingUser != null)
    {
        ViewBag.errorMessage = "User already exists";
        return View();
    }

    var oldPending = await dbContext.PendingRegistrations.FirstOrDefaultAsync(p => p.Email == model.Email);
    if (oldPending != null)
    {
        dbContext.PendingRegistrations.Remove(oldPending);
    }

    var otp = new Random().Next(100000, 999999).ToString();

    var pending = new PendingRegistration
    {
        Username = model.Username,
        Email = model.Email,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
        Otp = otp,
        OtpExpiry = DateTime.UtcNow.AddMinutes(10)
    };

    await dbContext.PendingRegistrations.AddAsync(pending);
    await dbContext.SaveChangesAsync();

    var body = $"<p>Hi {model.Username},</p>" +
                $"<p>Your WularTech verification code is:</p>" +
                $"<h2>{otp}</h2>" +
                $"<p>This code expires in 10 minutes.</p>";

    await emailService.SendEmailAsync(model.Email, "Verify your WularTech account", body);

    return RedirectToAction("VerifyOtp", new { email = model.Email });
}

[HttpGet]
public ActionResult VerifyOtp(string email)
{
    var model = new VerifyOtpViewModel { Email = email, Otp = "" };
    return View(model);
}

[HttpPost]
public async Task<ActionResult> VerifyOtp(VerifyOtpViewModel model)
{
    if (!ModelState.IsValid)
    {
        return View(model);
    }

    var pending = await dbContext.PendingRegistrations.FirstOrDefaultAsync(p => p.Email == model.Email);

    if (pending == null)
    {
        ViewBag.errorMessage = "No pending registration found. Please register again.";
        return View(model);
    }

    if (pending.OtpExpiry < DateTime.UtcNow)
    {
        ViewBag.errorMessage = "This code has expired. Please request a new one.";
        return View(model);
    }

    if (pending.Otp != model.Otp)
    {
        ViewBag.errorMessage = "Incorrect code. Please try again.";
        return View(model);
    }

    var user = new User
    {
        Username = pending.Username,
        Email = pending.Email,
        Password = pending.PasswordHash,
        IsAdmin = false
    };

    await dbContext.Users.AddAsync(user);
    dbContext.PendingRegistrations.Remove(pending);
    await dbContext.SaveChangesAsync();

    ViewBag.message = "Account verified! You can now log in.";
    return RedirectToAction("Login");
}

[HttpPost]
public async Task<ActionResult> ResendOtp(string email)
{
    var pending = await dbContext.PendingRegistrations.FirstOrDefaultAsync(p => p.Email == email);

    if (pending == null)
    {
        ViewBag.errorMessage = "No pending registration found. Please register again.";
        return RedirectToAction("Register");
    }

    var otp = new Random().Next(100000, 999999).ToString();
    pending.Otp = otp;
    pending.OtpExpiry = DateTime.UtcNow.AddMinutes(10);
    await dbContext.SaveChangesAsync();

    var body = $"<p>Hi {pending.Username},</p>" +
                $"<p>Your new WularTech verification code is:</p>" +
                $"<h2>{otp}</h2>" +
                $"<p>This code expires in 10 minutes.</p>";

    await emailService.SendEmailAsync(pending.Email, "Your new verification code", body);

    var model = new VerifyOtpViewModel { Email = email, Otp = "" };
    ViewBag.message = "A new code has been sent to your email.";
    return View("VerifyOtp", model);
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


        [HttpGet]
public ActionResult ForgotPassword()
{
    return View();
}

[HttpPost]
public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
{
    if (!ModelState.IsValid)
    {
        return View(model);
    }

    var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

    // Always show the same message whether or not the user exists,
    // so we don't leak which emails are registered.
    if (user != null)
    {
        var token = Guid.NewGuid().ToString("N");
        user.ResetToken = token;
        user.ResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        await dbContext.SaveChangesAsync();

        var resetLink = Url.Action("ResetPassword", "Account",
            new { email = user.Email, token = token }, Request.Scheme);

        var body = $"<p>Hi {user.Username},</p>" +
                    $"<p>Click the link below to reset your password. This link expires in 1 hour.</p>" +
                    $"<p><a href=\"{resetLink}\">Reset Password</a></p>" +
                    $"<p>If you didn't request this, you can ignore this email.</p>";

        await emailService.SendEmailAsync(user.Email, "Reset your WularTech password", body);
    }

    ViewBag.message = "If that email is registered, a reset link has been sent.";
    return View();
}

[HttpGet]
public async Task<ActionResult> ResetPassword(string email, string token)
{
    if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
    {
        return RedirectToAction("ForgotPassword");
    }

    var user = await dbContext.Users.FirstOrDefaultAsync(u =>
        u.Email == email && u.ResetToken == token);

    if (user == null || user.ResetTokenExpiry == null || user.ResetTokenExpiry < DateTime.UtcNow)
    {
        ViewBag.errorMessage = "This reset link is invalid or has expired.";
        return View("ResetPasswordInvalid");
    }

    var model = new ResetPasswordViewModel { Email = email, Token = token };
    return View(model);
}

[HttpPost]
public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
{
    if (!ModelState.IsValid)
    {
        return View(model);
    }

    var user = await dbContext.Users.FirstOrDefaultAsync(u =>
        u.Email == model.Email && u.ResetToken == model.Token);

    if (user == null || user.ResetTokenExpiry == null || user.ResetTokenExpiry < DateTime.UtcNow)
    {
        ViewBag.errorMessage = "This reset link is invalid or has expired.";
        return View("ResetPasswordInvalid");
    }

    user.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);
    user.ResetToken = null;
    user.ResetTokenExpiry = null;
    await dbContext.SaveChangesAsync();

    ViewBag.message = "Your password has been reset. You can now log in.";
    return RedirectToAction("Login");
}
















        [HttpPost]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("jwt");
            return RedirectToAction("Login", "Account");
        }
      
    }
}