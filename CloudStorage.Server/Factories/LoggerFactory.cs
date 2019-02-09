using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CloudStorage.Server.Data;
using CloudStorage.Server.Logging;

namespace CloudStorage.Server.Factories
{
    public class LoggerFactory : ILoggerFactory
    {
        public ILogger NewLogger(Logger loggerType)
        {
            switch (loggerType)
            {
                case Logger.AutomaticFileLogger:
                    return new AutomaticFileLogger();
                default:
                    return new AutomaticFileLogger();
            }
        }
    }
}
