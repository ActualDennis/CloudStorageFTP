using CloudStorage.Server.Data;
using CloudStorage.Server.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudStorage.Server.Factories
{
    public interface ILoggerFactory
    {
        ILogger NewLogger(Logger loggerType);
    }
}
