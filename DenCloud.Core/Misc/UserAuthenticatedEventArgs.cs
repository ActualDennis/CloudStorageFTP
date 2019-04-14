using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DenCloud.Core.Misc
{
    public class UserAuthenticatedEventArgs
    {
        public EndPoint EndPoint;
        public string UserName;
    }
}
