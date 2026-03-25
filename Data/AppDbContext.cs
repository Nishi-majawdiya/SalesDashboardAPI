using Microsoft.EntityFrameworkCore;
using SalesDashboardAPI.Models;


namespace SalesDashboardAPI.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Sales> Sales { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }
    }

}
