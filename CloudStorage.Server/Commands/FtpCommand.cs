using CloudStorage.Server.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudStorage.Server.Commands {
    public abstract class FtpCommand {
        public ControlConnection controlConnection { get; }

        public FtpCommand(ControlConnection controlConnection)
        {
            this.controlConnection = controlConnection;
        }

        public abstract Task<FtpReply> Execute(string parameter);

        protected FtpReply CheckUserInput(string parameter, bool checkForParameterNeeded)
        {
            if (!controlConnection.IsAuthenticated)
            {
                return new FtpReply { Message = "Log in with USER command first.", ReplyCode = FtpReplyCode.NotLoggedIn };
            }

            if (!checkForParameterNeeded)
                return null;

            if (string.IsNullOrWhiteSpace(parameter))
                return new FtpReply { Message = "No parameter was provided.", ReplyCode = FtpReplyCode.SyntaxErrorInParametersOrArguments };

            return null;
        }
    }
}
