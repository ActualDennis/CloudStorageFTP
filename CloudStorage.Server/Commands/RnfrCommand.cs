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
    public class RnfrCommand : FtpCommand {
        public RnfrCommand(
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

                var newCommand = await controlConnection.OnRenameFromCommandReceived();

                string newParam;
                var spaceIndex = newCommand.IndexOf(" ", StringComparison.Ordinal);

                if (spaceIndex.Equals(-1))
                {
                    return new FtpReply()
                    {
                        ReplyCode = FtpReplyCode.SyntaxErrorInParametersOrArguments,
                        Message = "No parameter for RNTO was provided."
                    };
                }

                newParam = newCommand.Substring(spaceIndex + 1);
                newCommand = newCommand.Substring(0, spaceIndex);

                if (newCommand != FtpCommands.RenameTo)
                {
                    return new FtpReply()
                    {
                        ReplyCode = FtpReplyCode.SyntaxErrorInParametersOrArguments,
                        Message = "RNFR is always followed by RNTO."
                    };
                }

                controlConnection.OnRename(parameter, newParam);

                return new FtpReply()
                {
                    ReplyCode = FtpReplyCode.FileActionOk,
                    Message = "Successfully renamed."
                };
            }
            catch (Exception ex)
            {
                if (ex is ObjectDisposedException)
                    return null;

                logger.Log(ex.Message, RecordKind.Error);

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
