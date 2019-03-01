using CloudStorage.Server.Authentication;
using CloudStorage.Server.Data;
using CloudStorage.Server.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudStorage.Server.Commands {
    public class UserCommand : FtpCommand {
        public UserCommand(
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
                var newCommand = await controlConnection.OnUserCommandReceived(parameter);

                if (newCommand == null)
                    return null;

                var spaceIndex = newCommand.IndexOf(" ", StringComparison.Ordinal);
                string password;

                if (spaceIndex.Equals(-1))
                {
                    return new FtpReply()
                    {
                        ReplyCode = FtpReplyCode.SyntaxErrorInParametersOrArguments,
                        Message = "No parameter for PASS was provided."
                    };
                }

                password = newCommand.Substring(spaceIndex + 1);
                newCommand = newCommand.Substring(0, spaceIndex);

                if (newCommand != FtpCommands.UserPassword)
                {
                    return new FtpReply()
                    {
                        ReplyCode = FtpReplyCode.SyntaxErrorInParametersOrArguments,
                        Message = "USER is always followed by PASS command."
                    };
                }

                if (!controlConnection.OnAuthenticateUser(parameter, password))
                {
                    return new FtpReply()
                    {
                        ReplyCode = FtpReplyCode.NotLoggedIn,
                        Message = "Wrong password."
                    };
                }

                await controlConnection.OnAuthenticated(parameter);

                return new FtpReply()
                {
                    ReplyCode = FtpReplyCode.UserLoggedIn,
                    Message = "Successfully logged in."
                };
            }
            catch(Exception ex)
            {
                logger.Log(ex.Message, RecordKind.Error);
                return null;
            }

        }
    }
}
