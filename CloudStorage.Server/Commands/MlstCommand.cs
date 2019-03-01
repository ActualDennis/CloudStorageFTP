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
    public class MlstCommand : FtpCommand {
        public MlstCommand(
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

                await SendFileorDirectoryInfo(parameter);

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

        private async Task SendFileorDirectoryInfo(string parameter)
        {
            var entries = controlConnection.FileSystemProvider.EnumerateDirectory(parameter);
            var memStream = new MemoryStream();
            var writer = new StreamWriter(memStream);

            foreach (var entry in entries)
            {
                var perms = (entry.EntryType == FileSystemEntryType.FILE) ? (entry.IsReadOnly ? "r" : "rw") : ("el");

                await writer.WriteLineAsync($"Type={((entry.EntryType == FileSystemEntryType.FILE) ? "file" : "dir")};" +
                    $"{(entry.EntryType == FileSystemEntryType.FILE ? "Size" : "Sizd")}" +
                    $"={entry.OccupiedSpace};Perm={perms};" +
                    $"Modify={entry.LastWriteTime.ToString("yyyyMMddhhmmss")}; {entry.Name}");
            }

            await writer.FlushAsync();

            memStream.Seek(0, SeekOrigin.Begin);
            await controlConnection.OnSendData(memStream);
            memStream.Close();
        }
    }
}
