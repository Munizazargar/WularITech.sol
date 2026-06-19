using Microsoft.EntityFrameworkCore;
using WularItech_solutions;
using WularItech_solutions.Configuration;
using WularItech_solutions.Interfaces;
using WularItech_solutions.Services;
using WularItech_solutions.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;

Console.WriteLine("=== APP STARTING ===");

var builder = WebApplication.CreateBuilder(args);

// ================= DATABASE CONFIG =================

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDbContext<SqlDbContext>(options =>
        options.UseSqlite("Data Source=WularItech.db"));
}
else
{
    var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");

    if (string.IsNullOrEmpty(connectionString))
        throw new Exception("DATABASE_URL is not set.");

    if (connectionString.StartsWith("postgres://"))
    {
        var uri = new Uri(connectionString);
        var userInfo = uri.UserInfo.Split(':');

        connectionString =
            $"Host={uri.Host};" +
            $"Port={uri.Port};" +
            $"Username={userInfo[0]};" +
            $"Password={userInfo[1]};" +
            $"Database={uri.AbsolutePath.TrimStart('/')};" +
            "SSL Mode=Require;Trust Server Certificate=true";
    }

    builder.Services.AddDbContext<SqlDbContext>(options =>
        options.UseNpgsql(connectionString));
}

// ================= OTHER SERVICES =================

builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.Configure<CloudinarySettings>(
    builder.Configuration.GetSection("CloudinarySettings"));

builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();

builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddDataProtection()
    .SetApplicationName("WularItechSolutions")
    .PersistKeysToDbContext<SqlDbContext>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// ================= MIDDLEWARE =================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// REMOVE app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ================= AUTO APPLY MIGRATIONS & SEED DATA =================

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SqlDbContext>();

    try
    {
        db.Database.Migrate();
        Console.WriteLine("=== MIGRATIONS APPLIED ===");
    }
    catch (Exception ex)
    {
        Console.WriteLine("=== MIGRATION ERROR: " + ex.Message);
    }

    try
    {
        if (!db.Products.Any())
        {
            var products = new List<Product>
            {
                new Product
                {
                    ProductName = "Hikvision CCTV Camera",
                    ProductImage = "https://placehold.co/600x400",
                    ProductDescription = "2MP Outdoor Security Camera with Night Vision.",
                    ProductPrice = 2500m,
                    ProductStock = 20
                },
                new Product
                {
                    ProductName = "Dahua CCTV Camera",
                    ProductImage = "https://placehold.co/600x400",
                    ProductDescription = "Full HD CCTV Camera for Home & Office Security.",
                    ProductPrice = 2200m,
                    ProductStock = 15
                },
                new Product
                {
                    ProductName = "4 Channel DVR",
                    ProductImage = "https://placehold.co/600x400",
                    ProductDescription = "Supports up to 4 CCTV Cameras with Remote Access.",
                    ProductPrice = 4500m,
                    ProductStock = 10
                },
                new Product
                {
                    ProductName = "8 Channel DVR",
                    ProductImage = "https://placehold.co/600x400",
                    ProductDescription = "High Performance DVR for Commercial Installations.",
                    ProductPrice = 6500m,
                    ProductStock = 8
                },
                new Product
                {
                    ProductName = "90 Meter CCTV Cable",
                    ProductImage = "https://placehold.co/600x400",
                    ProductDescription = "Premium Quality CCTV Cable Roll.",
                    ProductPrice = 1200m,
                    ProductStock = 30
                },
                new Product
                {
                    ProductName = "SMPS Power Supply",
                    ProductImage = "https://placehold.co/600x400",
                    ProductDescription = "12V Power Supply for CCTV Installations.",
                    ProductPrice = 800m,
                    ProductStock = 25
                }
            };

            db.Products.AddRange(products);
            db.SaveChanges();
            Console.WriteLine("=== PRODUCTS SEEDED ===");
        }
        else
        {
            Console.WriteLine("=== PRODUCTS ALREADY EXIST. SKIPPING SEED. ===");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("=== SEED ERROR: " + ex.Message);
    }
}

Console.WriteLine("=== REACHED app.Run() ===");

try
{

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<SqlDbContext>();
        db.Database.Migrate();
    }



    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine("=== FATAL STARTUP ERROR ===");
    Console.WriteLine(ex.GetType().FullName);
    Console.WriteLine(ex.Message);
    Console.WriteLine(ex.StackTrace);
    throw;
}