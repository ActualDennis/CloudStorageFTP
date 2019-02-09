namespace CloudStorage.Server.Authentication
{
    public interface IAuthenticationProvider
    {
        bool Authenticate(string username, string password);
    }
}