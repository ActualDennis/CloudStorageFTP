using CloudStorage.Server.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CloudStorage.Server.Commands {
    public class PasvCommand : FtpCommand {
        public PasvCommand(ControlConnection controlConnection) : base(controlConnection)
        {
        }

        public async override Task<FtpReply> Execute(string parameter)
        {
            try
            {
                var errorReply = CheckUserInput(parameter, false);
                if (errorReply != null) return errorReply;

                return new FtpReply()
                {
                    Message = controlConnection.OnEnterPassiveMode(),
                    ReplyCode = FtpReplyCode.EnteringPassiveMode
                };
            }
            catch (Exception ex)
            {
                if (ex is ObjectDisposedException)
                    return null;


                return new FtpReply() { Message = $"Error happened: {ex.Message}", ReplyCode = FtpReplyCode.LocalError };
            }
        }

    }
}
