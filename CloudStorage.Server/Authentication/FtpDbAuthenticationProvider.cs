using CloudStorage.Server.Helpers;

namespace CloudStorage.Server.Authentication
{
    /// <summary>
    ///     Basic authentication provider for ftp server
    ///     Supports Database authentication, anonymous authentication
    /// </summary>
    public class FtpDbAuthenticationProvider : IAuthenticationProvider
    {
        public bool Authenticate(string username, string password)
        {
            if (username == "anonymous") return true;

            using (var context = new ApplicationDbContext())
            {
                var user = context.Users.Find(Hasher.GetHash(username));

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
}