namespace DenCloud.Core.Authentication
{
    public interface IAuthenticationProvider
    {
        bool Authenticate(string username, string password);
    }
}