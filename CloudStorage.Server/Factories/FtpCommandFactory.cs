using CloudStorage.Server.Commands;
using CloudStorage.Server.Data;
using CloudStorage.Server.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudStorage.Server.Factories {
    public class FtpCommandFactory {
        public FtpCommand GetCommand(string command, ControlConnection connection,  ILogger logger)
        {
            switch (command)
            {
                case FtpCommands.ActiveConnection: return new ActiveCommand(connection);

                case FtpCommands.EnableEncryption: return new AuthCommand(connection, logger);

                case FtpCommands.GoUp: return new CdupCommand(connection, logger);

                case FtpCommands.ClientInfo: return new ClntCommand(connection, logger);

                case FtpCommands.ChangeWorkingDirectory: return new CwdCommand(connection, logger);

                case FtpCommands.DeleteFile: return new DeleCommand(connection, logger);

                case FtpCommands.ExtendedPassiveConnection: return new EpasvCommand(connection, logger);

                case FtpCommands.FeatureList: return new FeatCommand(connection);

                case FtpCommands.DirectoryListing: return new ListCommand(connection, logger);

                case FtpCommands.FileLastModifiedTime: return new MdtmCommand(connection, logger);

                case FtpCommands.CreateDirectory: return new MkdCommand(connection, logger);

                case FtpCommands.FileorDirectoryInfo: return new MlstCommand(connection, logger);

                case FtpCommands.ExtendedDirectoryListing: return new MlsdCommand(connection, logger);

                case FtpCommands.NameListing: return new NlstCommand(connection, logger);

                case FtpCommands.KeepAlive: return new NoopCommand(connection);

                case FtpCommands.Options: return new OptsCommand(connection);

                case FtpCommands.UserPassword: return new PassCommand(connection);

                case FtpCommands.PassiveConnection: return new PasvCommand(connection);

                case FtpCommands.DataChannelBufferSize: return new PbszCommand(connection);

                case FtpCommands.DataChannelProtection: return new ProtCommand(connection);

                case FtpCommands.PrintDirectory: return new PwdCommand(connection);

                case FtpCommands.Quit: return new QuitCommand(connection);

                case FtpCommands.DownloadFile: return new RetrCommand(connection, logger);

                case FtpCommands.RemoveDirectory: return new RmdCommand(connection, logger);

                case FtpCommands.RenameFrom: return new RnfrCommand(connection, logger);

                case FtpCommands.RenameTo: return new RntoCommand(connection);

                case FtpCommands.SiteSpecific: return new SiteCommand(connection, logger);

                case FtpCommands.Size: return new SizeCommand(connection, logger);

                case FtpCommands.UploadFile: return new StorCommand(connection, logger);

                case FtpCommands.SystemType: return new SystCommand(connection);

                case FtpCommands.ChangeTransferType: return new TypeCommand(connection);

                case FtpCommands.UserLogin: return new UserCommand(connection, logger);
                    
                default: return new UnrecognizedCommand(connection);
            }
        }
    }
}
