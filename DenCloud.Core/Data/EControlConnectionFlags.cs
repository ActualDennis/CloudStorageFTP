using System;

namespace DenCloud.Core.Data
{
    [Flags]
    public enum ControlConnectionFlags
    {
        UsingTLSorSSL,
        UTF8ON
    }
}