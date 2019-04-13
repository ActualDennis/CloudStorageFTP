using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudStorage.Server.Di
{
    [Flags]
    public enum DiConfigFlags
    {
        NecessaryClassesUsed,
        LoggerUsed,
        AuthUsed,
        FilesystemUsed
    }
}
