using CloudStorage.Server.Authentication;
using CloudStorage.Server.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudStorage.Server.Commands {
    public class ModeCommand : FtpCommand {
        public ModeCommand(
            ControlConnection controlConnection) : base(controlConnection)
        {

        }


        public async override Task<FtpReply> Execute(string parameter)
        {
            switch (parameter)
            {
                case "S":
                    return new FtpReply()
                    {
                        ReplyCode = FtpReplyCode.Okay,
                        Message = "Using stream mode"
                    };
                default:
                    return new FtpReply()
                    {
                        ReplyCode = FtpReplyCode.ParameterNotImplemented,
                        Message = "Not implemented yet."
                    };
            }
        }
    }
}
