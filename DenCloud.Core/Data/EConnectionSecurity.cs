using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DenCloud.Core.Data
{
    public enum ConnectionSecurity
    {
        ControlConnectionSecured,
        DataChannelSecured,
        Both,
        NonSecure
    }
}
