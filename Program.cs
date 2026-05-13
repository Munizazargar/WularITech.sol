using Microsoft.EntityFrameworkCore;
using WularItech_solutions;
using WularItech_solutions.Configuration;
using WularItech_solutions.Interfaces;
using WularItech_solutions.Services;
using Npgsql.EntityFrameworkCore.PostgreSQL;

var builder = WebApplication.CreateBuilder(args);

// ================= DATABASE CONFIG =================

if (builder.Environment.IsDevelopment())
{
    // Switch from SQL Server to SQLite for local development
    builder.Services.AddDbContext<SqlDbContext>(options =>
        options.UseSqlite("Data Source=WularItech.db")); 
}
else
{
    // Railway PostgreSQL (Production) - Keep this as is
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

    if (!string.IsNullOrEmpty(databaseUrl))
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':');

        var connectionString =
            $"Host={uri.Host};" +
            $"Port={uri.Port};" +
            $"Database={uri.AbsolutePath.TrimStart('/')};" +
            $"Username={userInfo[0]};" +
            $"Password={userInfo[1]};" +
            $"SSL Mode=Require;Trust Server Certificate=true";

        builder.Services.AddDbContext<SqlDbContext>(options =>
            options.UseNpgsql(connectionString));
    }
    else
    {
        throw new Exception("DATABASE_URL is not set.");
    }
}

// ================= OTHER SERVICES =================

builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.Configure<CloudinarySettings>(
    builder.Configuration.GetSection("CloudinarySettings"));

builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// ================= MIDDLEWARE =================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// 1. Replace app.MapStaticAssets() with UseStaticFiles()
app.UseStaticFiles(); 

app.UseRouting();
app.UseAuthorization();

// 2. Remove .WithStaticAssets() from the route mapping
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


// ================= AUTO APPLY MIGRATIONS =================

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SqlDbContext>();
    db.Database.Migrate();
}

app.Run();
