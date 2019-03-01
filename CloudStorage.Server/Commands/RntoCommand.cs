using CloudStorage.Server.Authentication;
using CloudStorage.Server.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudStorage.Server.Commands {
    public class RntoCommand : FtpCommand {
        public RntoCommand(
            ControlConnection controlConnection) : base(controlConnection)
        {

        }


        public async override Task<FtpReply> Execute(string parameter)
        {
            return new FtpReply()
            {
                ReplyCode = FtpReplyCode.SyntaxErrorInParametersOrArguments,
                Message = "RNFR is always followed by RNTO command."
            };
        }
    }
}
