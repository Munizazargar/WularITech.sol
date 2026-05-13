using Microsoft.EntityFrameworkCore;
using WularItech_solutions.Models;
namespace WularItech_solutions
{
    public class SqlDbContext : DbContext
    {
        public SqlDbContext(DbContextOptions<SqlDbContext> options) : base(options) { }

        //entities
        public DbSet<User> Users { get; set; }
        public DbSet<Product>Products{get;set;}

        public DbSet<Cart>Carts{get;set;}
        public DbSet<Contact> Contacts { get; set; }

    }
}
