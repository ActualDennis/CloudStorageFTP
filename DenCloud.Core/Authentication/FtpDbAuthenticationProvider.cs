using DenCloud.Core.Helpers;
using System.Data.Entity;

namespace DenCloud.Core.Authentication
{
    /// <summary>
    ///     Basic authentication provider for ftp server
    ///     Supports Database authentication, anonymous authentication
    /// </summary>
    public class FtpDbAuthenticationProvider : IAuthenticationProvider
    {
        public FtpDbAuthenticationProvider(ApplicationDbContext db)
        {
            this.db = db;
            db.Database.CreateIfNotExists();
        }

        private ApplicationDbContext db { get; set; }

        public bool Authenticate(string username, string password)
        {
            if (username == "anonymous") return true;

            var user = db.Users.Find(Hasher.GetHash(username));

            if (user == null)
                return false;

            if (user.IsDisabled)
                return false;

            var passwdHash = Hasher.GetHash(password);

            if (passwdHash.Equals(user.PasswordHash))
                return true;

            return false;
        }
    }
}