using System.Data.Entity;

namespace DenCloud.Core.Authentication
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<FtpUser> Users { get; set; }

    }
}