using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudStorage.Server.Data
{
    public enum ConnectionSecurity
    {
        ControlConnectionSecured,
        DataChannelSecured,
        Both,
        NonSecure
    }
}
