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
    public class ListCommand : FtpCommand {
        public ListCommand(
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

                await SendDirectoryListing();

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

        private async Task SendDirectoryListing()
        {
            var entries = controlConnection.FileSystemProvider.EnumerateDirectory(null);

            var memStream = new MemoryStream();

            var writer = new StreamWriter(memStream);

            foreach (var entry in entries)
            {
                var x = string.Format(
                    "{0}{1}{1}{1}   1 owner   group {2,15} {3} {4}",
                    entry.EntryType.Equals(FileSystemEntryType.FOLDER) ? 'd' : '-',
                    entry.IsReadOnly ? "r-x" : "rwx",
                    entry.OccupiedSpace,
                    entry.LastWriteTime.ToString(
                        entry.LastWriteTime.Year == DateTime.Now.Year ? "MMM dd HH:mm" : "MMM dd  yyyy",
                        CultureInfo.InvariantCulture),
                    entry.Name);
                await writer.WriteLineAsync(x);
            }

            writer.Flush();
            memStream.Seek(0, SeekOrigin.Begin);
            await controlConnection.OnSendData(memStream);
            memStream.Close();
        }
    }
}
