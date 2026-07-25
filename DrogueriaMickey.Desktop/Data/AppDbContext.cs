using Microsoft.EntityFrameworkCore;
using DrogueriaMickey.Desktop.Models;

namespace DrogueriaMickey.Desktop.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<SyncEvent> SyncEvents => Set<SyncEvent>();
        public DbSet<User> Users => Set<User>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=drogueria_micky.db;Cache=Shared");
        }
    }
}