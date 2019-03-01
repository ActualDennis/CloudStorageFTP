using CloudStorage.Server.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CloudStorage.Server.Commands {
    public class ActiveCommand : FtpCommand {
        public ActiveCommand(ControlConnection controlConnection) : base(controlConnection)
        {
        }

        public async override Task<FtpReply> Execute(string parameter)
        {
            try
            {
                var errorReply = CheckUserInput(parameter, true);
                if (errorReply != null) return errorReply;

                controlConnection.OnEnterActiveMode(parameter);

                return new FtpReply() { ReplyCode = FtpReplyCode.Okay, Message = "Ready to communicate." };
            }
            catch(Exception ex)
            {
                if (ex is ObjectDisposedException)
                    return null;

                return new FtpReply() { Message = $"Error happened: {ex.Message}", ReplyCode = FtpReplyCode.LocalError };
            }
        }
    }
}
