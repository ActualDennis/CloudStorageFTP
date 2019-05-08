
using Microsoft.EntityFrameworkCore;

namespace DenCloud.Core.Authentication
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<FtpUser> Users { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseSqlite(@"Data Source=FtpUsers.db;");
        }

    }
}