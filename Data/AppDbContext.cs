using InventarioMvc.Models;
using Microsoft.EntityFrameworkCore;

namespace InventarioMvc.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Sale> Sales => Set<Sale>();
    }
}
