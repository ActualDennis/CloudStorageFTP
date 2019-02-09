using System.Data.Entity;

namespace CloudStorage.Server.Authentication
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<FtpUser> Users { get; set; }

    }
}