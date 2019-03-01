using CloudStorage.Server.Data;
using CloudStorage.Server.Logging;
using CloudStorage.Server.Misc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace CloudStorage.Server.Commands {
    public class AuthCommand : FtpCommand {
        public AuthCommand(ControlConnection controlConnection, ILogger logger) : base(controlConnection)
        {
        }

        public async override Task<FtpReply> Execute(string parameter)
        {
            if (string.IsNullOrEmpty(parameter))
            {
               return new FtpReply() { ReplyCode = FtpReplyCode.BadSequence, Message = "No parameter was provided." };
            }

            switch (parameter)
            {
                case "SSL":
                case "TLS":
                    {
                        await controlConnection.OnEncryptionEnabled();
                        break;
                    }
            }

            return null;
        }
    }
}
