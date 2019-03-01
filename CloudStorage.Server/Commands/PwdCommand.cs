using CloudStorage.Server.Authentication;
using CloudStorage.Server.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudStorage.Server.Commands {
    public class PwdCommand : FtpCommand {
        public PwdCommand(
            ControlConnection controlConnection) : base(controlConnection)
        {

        }


        public async override Task<FtpReply> Execute(string parameter)
        {
            if (!controlConnection.IsAuthenticated)
            {
                return new FtpReply()
                {
                    ReplyCode = FtpReplyCode.NotLoggedIn,
                    Message = "Log in with USER command first."
                };
            }

            return new FtpReply()
            {
                ReplyCode = FtpReplyCode.PathCreated,
                Message = $"\"{controlConnection.FileSystemProvider.WorkingDirectory}\""
            };
        }
    }
}
