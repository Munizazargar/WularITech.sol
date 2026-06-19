using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WularItech_solutions.Models;

namespace WularItech_solutions
{
    public class SqlDbContext : DbContext, IDataProtectionKeyContext
    {
        public SqlDbContext(DbContextOptions<SqlDbContext> options) : base(options) { }

        // entities
        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<Booking> Bookings { get; set; }

        // Data Protection keys — persists antiforgery keys to PostgreSQL
        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

        public DbSet<Technician> Technicians { get; set; }
    }
}
