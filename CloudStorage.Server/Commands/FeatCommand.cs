using CloudStorage.Server.Authentication;
using CloudStorage.Server.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudStorage.Server.Commands {
    public class FeatCommand : FtpCommand {
        public FeatCommand(
            ControlConnection controlConnection) : base(controlConnection)
        {
            
        }


        public async override Task<FtpReply> Execute(string parameter)
        {
            controlConnection.OnSendFeatureList(new FtpReply()
            {
                ReplyCode = FtpReplyCode.SystemTypeName,
                Message = ReplyFeatureList(FtpReplyCode.SystemStatus, "Features: \n\rUTF8 \n\rTVFS \n\rSIZE \n\rMLST Type*;Size*;Perm*;Modify*;")
            });

            return null;
        }

        private string ReplyFeatureList(FtpReplyCode code, string message)
        {
            //Every line of reply should have leading space
            message = message.Replace("\r", " ");

            //this is required by specification
            message = message.Replace("\n", "\r\n");

            return $"{((int)code).ToString()}-{message}\r\n{((int)code).ToString()} End\r\n";
        }
    }
}
