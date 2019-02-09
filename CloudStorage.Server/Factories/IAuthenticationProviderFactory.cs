using CloudStorage.Server.Authentication;
using CloudStorage.Server.Data;

namespace CloudStorage.Server.Factories
{
    public interface IAuthenticationProviderFactory
    {
        IAuthenticationProvider NewAuthenticationProvider(AuthenticationProvider providerType);
    }
}
