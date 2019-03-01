using CloudStorage.Server.Authentication;
using CloudStorage.Server.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudStorage.Server.Commands {
    public class OptsCommand : FtpCommand {
        public OptsCommand(
            ControlConnection controlConnection) : base(controlConnection)
        {

        }


        public async override Task<FtpReply> Execute(string parameter)
        {
            if (string.IsNullOrEmpty(parameter))
            {
                return new FtpReply()
                {
                    ReplyCode = FtpReplyCode.SyntaxErrorInParametersOrArguments,
                    Message = "No parameter was provided."
                };
            }

            if (!parameter.IndexOf("UTF8").Equals(-1))
            {
                if (parameter.IndexOf(" ").Equals(-1))
                {
                    return new FtpReply()
                    {
                        ReplyCode = FtpReplyCode.SyntaxErrorInParametersOrArguments,
                        Message = "Use ON/OFF keywords to enable/disable UTF8."
                    };
                }

                var secondaryCommand = parameter.Substring(parameter.IndexOf(" ") + 1);

                switch (secondaryCommand)
                {
                    case "ON":
                        {
                            controlConnection.OnUtf8Enabled();

                            return new FtpReply()
                            {
                                ReplyCode = FtpReplyCode.Okay,
                                Message = "Now using UTF8 encoding."
                            };
                        }
                    case "OFF":
                        {
                            controlConnection.OnUtf8Disabled();

                            return new FtpReply()
                            {
                                ReplyCode = FtpReplyCode.Okay,
                                Message = "Now using ASCII encoding."
                            };
                        }
                    default:
                        {
                            return new FtpReply()
                            {
                                ReplyCode = FtpReplyCode.SyntaxErrorInParametersOrArguments,
                                Message = "Use ON/OFF keywords to enable/disable UTF8."
                            };
                        }
                }
            }

            return null;
        }
    }
}
