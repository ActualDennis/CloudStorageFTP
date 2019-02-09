using CloudStorage.Server.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudStorage.Server.Logging
{
    public interface ILogger
    {
        void Log(string message, RecordKind kindOfRecord);
    }
}
