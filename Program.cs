using Microsoft.EntityFrameworkCore;
using WularItech_solutions;
using WularItech_solutions.Configuration;
using WularItech_solutions.Interfaces;
using WularItech_solutions.Services;
using WularItech_solutions.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;

// Simple startup marker — shows up first in Render logs so we can confirm
// the process actually began executing (useful when diagnosing crash-on-boot issues).
Console.WriteLine("=== APP STARTING ===");

// Creates the WebApplication builder — this is where all services (DI),
// configuration (appsettings.json + env vars), and logging get wired up
// before the app is actually built.
var builder = WebApplication.CreateBuilder(args);

// ================= DATABASE CONFIG =================
// Two different database providers depending on environment:
// - Local development uses SQLite (a single file, zero setup needed).
// - Production (Render) uses Neon Postgres, configured via the DATABASE_URL
//   environment variable that Render/Neon provides.

if (builder.Environment.IsDevelopment())
{
    // Local dev: lightweight file-based SQLite database, no external dependency.
    builder.Services.AddDbContext<SqlDbContext>(options =>
        options.UseSqlite("Data Source=WularItech.db"));
}
else
{
    // Production: read the Postgres connection string from the environment.
    // This is intentionally NOT hardcoded or stored in appsettings.json —
    // it's injected by Render at runtime as a secret.
    var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");

    // Fail fast and loud if the environment variable is missing — better to
    // crash immediately with a clear message than silently fail later.
    if (string.IsNullOrEmpty(connectionString))
        throw new Exception("DATABASE_URL is not set.");

    // Neon (and most cloud Postgres providers) hand out connection strings in
    // URI format: postgres://user:password@host:port/dbname
    // Npgsql (the .NET Postgres driver) expects key=value format instead, so
    // we parse the URI manually and rebuild it in the format Npgsql understands.
    if (connectionString.StartsWith("postgres://"))
    {
        var uri = new Uri(connectionString);
        var userInfo = uri.UserInfo.Split(':'); // userInfo[0] = username, userInfo[1] = password

        connectionString =
            $"Host={uri.Host};" +
            $"Port={uri.Port};" +
            $"Username={userInfo[0]};" +
            $"Password={userInfo[1]};" +
            $"Database={uri.AbsolutePath.TrimStart('/')};" + // path starts with "/", strip it to get just the db name
            "SSL Mode=Require;Trust Server Certificate=true"; // Neon requires SSL connections
    }

    // Register the DbContext using the Postgres provider with our rebuilt connection string.
    builder.Services.AddDbContext<SqlDbContext>(options =>
        options.UseNpgsql(connectionString));
}

// ================= OTHER SERVICES =================
// Dependency injection registrations — these make the listed interfaces
// available to be injected into controllers/services throughout the app.

// Handles JWT creation/validation for both admin/customer (User) and Technician auth.
builder.Services.AddScoped<ITokenService, TokenService>();

// Binds the "CloudinarySettings" section of configuration (appsettings.json / env vars)
// to the strongly-typed CloudinarySettings class, so it can be injected via IOptions<CloudinarySettings>.
builder.Services.Configure<CloudinarySettings>(
    builder.Configuration.GetSection("CloudinarySettings"));

// Handles image upload/storage via Cloudinary (used presumably for product images).
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();

// Handles outgoing transactional emails via SendGrid (password reset, OTP verification, etc).
builder.Services.AddScoped<IEmailService, EmailService>();

// ASP.NET Core's Data Protection system is used internally for things like
// antiforgery tokens. By default, encryption keys are stored in memory/disk,
// which breaks across container restarts or multiple instances (e.g. on Render).
// PersistKeysToDbContext stores those keys in Postgres instead, so they survive
// restarts and redeploys — this is why a DataProtectionKeys table/DbSet exists.
builder.Services.AddDataProtection()
    .SetApplicationName("WularItechSolutions")
    .PersistKeysToDbContext<SqlDbContext>();

// Enables traditional MVC: Controllers + Razor Views (as opposed to minimal APIs).
builder.Services.AddControllersWithViews();

// All services are registered — build the actual app/request pipeline object.
var app = builder.Build();

// ================= MIDDLEWARE =================
// Middleware order matters here — each of these wraps every incoming request.

if (!app.Environment.IsDevelopment())
{
    // In production, don't show detailed exception pages (security risk) —
    // redirect to a friendly error page instead.
    app.UseExceptionHandler("/Home/Error");

    // Adds the Strict-Transport-Security (HSTS) response header, telling
    // browsers to only ever connect to this site over HTTPS in the future.
    app.UseHsts();
}

// NOTE: HTTPS redirection middleware is intentionally disabled here.
// This likely caused a redirect loop or similar issue with Render's reverse proxy
// (Render terminates SSL at its edge/load balancer, then forwards plain HTTP
// internally to the app — UseHttpsRedirection() can misfire in that setup
// without UseForwardedHeaders() configured first).
// REMOVE app.UseHttpsRedirection();

// Serves static files (css, js, images) from wwwroot.
app.UseStaticFiles();

// Resolves the incoming request URL to a matching controller/action route.
app.UseRouting();

// Enforces [Authorize]-style access rules (note: this app currently relies
// mostly on manual JWT cookie checks inside controllers, e.g. IsAdmin(),
// rather than the built-in ASP.NET Core [Authorize] attribute system).
app.UseAuthorization();

// Default MVC routing pattern: /{Controller}/{Action}/{optional id}
// e.g. /Account/Login, /Admin/Bookings, /Booking/Create/123
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ================= AUTO APPLY MIGRATIONS (DEV ONLY) =================

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SqlDbContext>();

    if (app.Environment.IsDevelopment())
    {
        // Auto-migrate only in local dev (SQLite). Production (Neon Postgres)
        // migrations are applied manually via the Neon SQL Editor, with the
        // __EFMigrationsHistory table updated by hand to match — this avoids
        // EF re-running SQL that's already been hand-applied and crashing on boot.
        try
        {
            db.Database.Migrate();
            Console.WriteLine("=== MIGRATIONS APPLIED (DEV) ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine("=== MIGRATION ERROR: " + ex.Message);
        }
    }
    else
    {
        Console.WriteLine("=== SKIPPING AUTO-MIGRATE IN PRODUCTION (manual Neon workflow) ===");
    }
}

Console.WriteLine("=== REACHED app.Run() ===");

app.Run();

// Marks that startup logic completed and we're about to hand control to the
// request-handling loop. Useful log marker to confirm the app didn't crash
// before reaching this point.
Console.WriteLine("=== REACHED app.Run() ===");

try
{
    // NOTE: This duplicates the Database.Migrate() call already performed above.
    // Unlike the first call, this one is NOT wrapped in a way that survives
    // failure — any exception thrown here is caught below only to be logged
    // and then re-thrown via `throw;`, which crashes the entire process.
    // This is the actual root cause of recent production crashes: a migration
    // mismatch here (e.g. EF trying to reapply a migration whose SQL was
    // already hand-applied in Neon) takes down the whole app on every boot
    // until manually resolved in the database.
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<SqlDbContext>();
        db.Database.Migrate();
    }

    // Starts the Kestrel web server and begins accepting incoming HTTP requests.
    // This call blocks for the lifetime of the application.
    app.Run();
}
catch (Exception ex)
{
    // Last-resort crash logging: prints full exception details to Render logs
    // before re-throwing, which terminates the process (Render will then
    // attempt to restart it, repeating the same failure if the underlying
    // migration issue isn't fixed first).
    Console.WriteLine("=== FATAL STARTUP ERROR ===");
    Console.WriteLine(ex.GetType().FullName);
    Console.WriteLine(ex.Message);
    Console.WriteLine(ex.StackTrace);
    throw;
}