using CloudStorage.Server.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudStorage.Server.Misc
{
    public class UserInformation
    {
        public string UserName;

        public bool IsAuthenticated;

        public ConnectionSecurity Security;
    }
}
