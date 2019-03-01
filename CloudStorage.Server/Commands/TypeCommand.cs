using CloudStorage.Server.Authentication;
using CloudStorage.Server.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudStorage.Server.Commands {
    public class TypeCommand : FtpCommand {
        public TypeCommand(
            ControlConnection controlConnection) : base(controlConnection)
        {

        }


        public async override Task<FtpReply> Execute(string parameter)
        {
            switch (parameter)
            {
                case "A":
                    return new FtpReply()
                    {
                        ReplyCode = FtpReplyCode.Okay,
                        Message = "Now using ascii type for transferring data."
                    };
                case "I":
                    return new FtpReply()
                    {
                        ReplyCode = FtpReplyCode.Okay,
                        Message = "Now using binary type for transferring data."
                    };
                default:
                    return new FtpReply()
                    {
                        ReplyCode = FtpReplyCode.ParameterNotImplemented,
                        Message = "Unknown type"
                    };
            }
        }
    }
}
