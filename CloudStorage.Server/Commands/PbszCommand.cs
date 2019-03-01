using CloudStorage.Server.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudStorage.Server.Commands {
    class PbszCommand : FtpCommand {
        public PbszCommand(ControlConnection controlConnection) : base(controlConnection)
        {
        }

        public async override Task<FtpReply> Execute(string parameter)
        {
            var errorReply = CheckUserInput(parameter, false);
            if (errorReply != null) return errorReply;

            int.TryParse(parameter, out var result);
            result = controlConnection.OnDataBufferSizeChanged(result);
            return new FtpReply() { ReplyCode = FtpReplyCode.Okay , Message = $"Buffer size was set to {result}." };
        }
    }
}
