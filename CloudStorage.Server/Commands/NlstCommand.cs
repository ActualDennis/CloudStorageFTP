using CloudStorage.Server.Authentication;
using CloudStorage.Server.Data;
using CloudStorage.Server.FileSystem;
using CloudStorage.Server.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudStorage.Server.Commands {
    public class NlstCommand : FtpCommand {
        public NlstCommand(
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

                await SendDirectoryNamesListing();

                return new FtpReply()
                {
                    ReplyCode = FtpReplyCode.SuccessClosingDataConnection,
                    Message = "Successfully sent file system listing."
                };
            }
            catch (Exception ex)
            {
                if (ex is ObjectDisposedException)
                    return null;

                logger.Log(ex.Message, RecordKind.Error);

                if ((ex is FormatException)
                    || (ex is InvalidOperationException)
                    || (ex is DirectoryNotFoundException)
                    || (ex is FileNotFoundException))
                {
                    return new FtpReply()
                    {
                        ReplyCode = FtpReplyCode.FileNoAccess,
                        Message = ex.Message
                    };
                }

                if ((ex is UnauthorizedAccessException)
                  || (ex is IOException))
                    return new FtpReply()
                    {
                        ReplyCode = FtpReplyCode.FileBusy,
                        Message = ex.Message
                    };

                return new FtpReply() { Message = $"Error happened: {ex.Message}", ReplyCode = FtpReplyCode.LocalError };
            }
        }

        private async Task SendDirectoryNamesListing()
        {
            var entries = controlConnection.FileSystemProvider.EnumerateDirectory(null);

            var memStream = new MemoryStream();

            var writer = new StreamWriter(memStream);

            foreach (var entry in entries)
            {
                if (entry.EntryType == FileSystemEntryType.FILE)
                    await writer.WriteLineAsync(entry.Name);
            }

            writer.Flush();
            memStream.Seek(0, SeekOrigin.Begin);
            await controlConnection.OnSendData(memStream);
            memStream.Close();
        }
    }
}
