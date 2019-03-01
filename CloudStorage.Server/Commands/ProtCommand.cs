using CloudStorage.Server.Data;
using CloudStorage.Server.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudStorage.Server.Commands {
    public class ProtCommand : FtpCommand {
        public ProtCommand(ControlConnection controlConnection) : base(controlConnection)
        {
        }

        public async override Task<FtpReply> Execute(string parameter)
        {
            var errorReply = CheckUserInput(parameter, true);
            if (errorReply != null) return errorReply;

            switch (parameter)
            {
                case "P":
                    {
                        await controlConnection.OnDataChannelEncryptionEnabled();
                        return new FtpReply()
                        {
                            ReplyCode = FtpReplyCode.Okay,
                            Message = "Successfully enabled encryption. You can now open data connection."
                        };

                    }
                case "C":
                    {
                        await controlConnection.OnDataChannelEncryptionDisabled();

                        return new FtpReply()
                        {
                            ReplyCode = FtpReplyCode.Okay,
                            Message = "Warning! Now using plain unencrypted way of data transmission. Consider using PROT P instead."
                        };
                    }
                default:
                    {
                        return new FtpReply()
                        {
                            ReplyCode = FtpReplyCode.NotImplemented,
                            Message = "Use either of two: PROT P or PROT C."
                        };
                    }
            }
        }
    }
}
