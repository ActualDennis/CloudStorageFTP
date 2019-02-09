using CloudStorage.Server.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudStorage.Server.Logging
{
    public abstract class FileLogger : ILogger
    {
        private const string loggingFile = "CloudStorageFTP.logs.txt";
        /// <summary>
        /// Value used by siblings to disable logging
        /// </summary>
        protected bool _isLoggingEnabled { get; set; } = true;

        private static object padLock = new object();

        public void Log(string message, RecordKind kindOfRecord)
        {
            if (!_isLoggingEnabled)
                return;

            var fullLogFilePath = DefaultServerValues.LoggingPath + Path.DirectorySeparatorChar + loggingFile;

            lock (padLock)
            {
                using (StreamWriter sWriter = new StreamWriter(fullLogFilePath, true))
                {
                    sWriter.Write(GetFormattedLogText(message, kindOfRecord));
                }
            }
        }

        private string GetFormattedLogText(string message, RecordKind kindOfRecord)
        {
            string value = kindOfRecord switch
            {
                RecordKind.CommandReceived => "Received a command",
                RecordKind.Error => "Error",
                RecordKind.Status => "Status",
                _ => "Status"
            };

            return $"{DateTime.Now.ToString()} : {value} : {message} {Environment.NewLine}";
        }
    }
}
