using DenCloud.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DenCloud.Core.Logging
{
    public interface ILogger
    {
        void Log(string message, RecordKind kindOfRecord);
    }
}
