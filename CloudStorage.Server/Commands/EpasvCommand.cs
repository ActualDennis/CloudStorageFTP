using CloudStorage.Server.Data;
using CloudStorage.Server.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CloudStorage.Server.Commands {
    public class EpasvCommand : FtpCommand {
        public EpasvCommand(
            ControlConnection controlConnection,
             ILogger logger) : base(controlConnection)
        {
            this.logger = logger;
        }

        ILogger logger { get; set; }

        public async override Task<FtpReply> Execute(string parameter)
        {
            try
            {
                var errorReply = CheckUserInput(parameter, false);
                if (errorReply != null) return errorReply;

                return EnterExtendedPassiveMode();
            }
            catch (Exception ex)
            {
                if (ex is ObjectDisposedException)
                    return null;

                logger.Log(ex.Message, RecordKind.Error);

                return new FtpReply() { Message = $"Error happened: {ex.Message}", ReplyCode = FtpReplyCode.LocalError };
            }
        }

        private FtpReply EnterExtendedPassiveMode()
        {
            var listeningPort = controlConnection.ClientDataConnection.InitializePassiveConnection();

            return new FtpReply()
            {
                ReplyCode = FtpReplyCode.EnteringExtendedPassiveMode,
                Message = $"Entering Extended Passive Mode (|||{listeningPort.ToString()}|)"
            };
        }
    }
}
