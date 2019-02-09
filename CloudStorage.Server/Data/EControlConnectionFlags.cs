using System;

namespace CloudStorage.Server.Data
{
    [Flags]
    public enum ControlConnectionFlags
    {
        UsingTLSorSSL,
        UTF8ON
    }
}