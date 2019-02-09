using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CloudStorage.Server.Authentication;
using CloudStorage.Server.Data;

namespace CloudStorage.Server.Factories
{
    class AuthenticationProviderFactory : IAuthenticationProviderFactory
    {
        public IAuthenticationProvider NewAuthenticationProvider(AuthenticationProvider providerType)
        {
            switch (providerType)
            {
                case AuthenticationProvider.Default:
                    return new FtpDbAuthenticationProvider();
                default:
                    return new FtpDbAuthenticationProvider();
            }
        }
    }
}
