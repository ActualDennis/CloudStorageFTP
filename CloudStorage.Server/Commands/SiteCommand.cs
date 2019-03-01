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
    public class SiteCommand : FtpCommand {
        public SiteCommand(
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

                if (string.IsNullOrEmpty(parameter))
                {
                    return new FtpReply()
                    {
                        ReplyCode = FtpReplyCode.SyntaxErrorInParametersOrArguments,
                        Message = "No parameter was provided."
                    };
                }

                var commandWords = parameter.Split(' ');

                switch (commandWords[0])
                { //To register , user should log in as anonymous and send example command:
                  //SITE REG *username* *password*
                    case LocalFtpCommands.Register:
                        {
                            if (commandWords.Length < 2 + 1)
                            {
                                return new FtpReply()
                                {
                                    ReplyCode = FtpReplyCode.SyntaxErrorInParametersOrArguments,
                                    Message = "Example usage: SITE REG *username* *password*. Consider encrypting your connection before doing this."
                                };
                            }

                            await controlConnection.OnUserRegistered(commandWords);

                            return new FtpReply()
                            {
                                ReplyCode = FtpReplyCode.Okay,
                                Message = $"Successfully registered."
                            };
                        }
                }

                return new FtpReply()
                {
                    ReplyCode = FtpReplyCode.SuccessClosingDataConnection,
                    Message = "Transfer complete."
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
