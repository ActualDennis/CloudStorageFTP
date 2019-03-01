using CloudStorage.Server.Authentication;
using CloudStorage.Server.Data;
using CloudStorage.Server.FileSystem;
using CloudStorage.Server.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudStorage.Server.Commands {
    public class CwdCommand : FtpCommand {
        public CwdCommand(
            ControlConnection controlConnection,
             ILogger logger) : base(controlConnection)
        {
            this.logger = logger;
        }

        ILogger logger { get; set; }

        public async override Task<FtpReply> Execute(string parameter)
        {
            try
            {
                var errorReply = CheckUserInput(parameter, true);
                if (errorReply != null) return errorReply;

                controlConnection.OnSetWorkingDirectory(parameter);

                return new FtpReply()
                {
                    ReplyCode = FtpReplyCode.Okay,
                    Message = $"Changed directory to {parameter}"
                };
            }
            catch (Exception ex)
            {
                if (ex is ObjectDisposedException)
                    return null;

                if ((ex is FormatException)
                    || (ex is InvalidOperationException)
                    || (ex is DirectoryNotFoundException)
                    || (ex is FileNotFoundException))
                {
                    return new FtpReply()
                    {
                        ReplyCode = FtpReplyCode.FileNoAccess,
                        Message = ex.Message
                    };
                }

                logger.Log(ex.Message, RecordKind.Error);

                if ((ex is UnauthorizedAccessException)
                  || (ex is IOException))
                    return new FtpReply()
                    {
                        ReplyCode = FtpReplyCode.FileBusy,
                        Message = ex.Message
                    };

                return new FtpReply() { Message = $"Error happened: {ex.Message}", ReplyCode = FtpReplyCode.LocalError };
            }
        }
    }
}
