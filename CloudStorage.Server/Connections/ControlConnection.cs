using CloudStorage.Server.Authentication;
using CloudStorage.Server.Commands;
using CloudStorage.Server.Connections;
using CloudStorage.Server.Data;
using CloudStorage.Server.Exceptions;
using CloudStorage.Server.FileSystem;
using CloudStorage.Server.Helpers;
using CloudStorage.Server.Logging;
using CloudStorage.Server.Misc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CloudStorage.Server
{
    /// <summary>
    /// Class used to accept commands from ftp client and execute them.
    /// </summary>
    public class ControlConnection : IDisposable {
        #region Constructor
        public ControlConnection(TcpClient connectedClient,
            IAuthenticationProvider authenticationProvider,
            ICloudStorageFileSystemProvider fileSystemProvider,
            ILogger logger)
        {
            ConnectedClient = connectedClient;
            ClientCommandStream = connectedClient.GetStream();
            CommandStreamReader = new StreamReader(ClientCommandStream, ServerEncoding);
            ClientDataConnection = new DataConnection(logger, fileSystemProvider);
            AuthenticationProvider = authenticationProvider;
            FileSystemProvider = fileSystemProvider;
            ClientInitialRemoteEndPoint = (IPEndPoint)connectedClient.Client.RemoteEndPoint;
            Logger = logger;

            Commands = new Dictionary<string, FtpCommand>()
            {
                {FtpCommands.ActiveConnection,new ActiveCommand(this) },
                {FtpCommands.EnableEncryption, new AuthCommand(this, Logger) },
                {FtpCommands.GoUp, new CdupCommand(this, Logger) },
                {FtpCommands.ClientInfo, new ClntCommand(this, Logger) },
                {FtpCommands.ChangeWorkingDirectory, new CwdCommand(this, Logger) },
                {FtpCommands.DeleteFile, new DeleCommand(this, Logger) },
                {FtpCommands.ExtendedPassiveConnection, new EpasvCommand(this, Logger) },
                {FtpCommands.FeatureList,new FeatCommand(this) },
                {FtpCommands.DirectoryListing,new ListCommand(this, Logger) },
                {FtpCommands.FileLastModifiedTime,new MdtmCommand(this, Logger) },
                {FtpCommands.CreateDirectory,new MkdCommand(this, Logger) },
                {FtpCommands.FileorDirectoryInfo,new MlstCommand(this, Logger) },
                {FtpCommands.ExtendedDirectoryListing,new MlsdCommand(this, Logger) },
                {FtpCommands.NameListing,new NlstCommand(this, Logger) },
                {FtpCommands.KeepAlive,new NoopCommand(this) },
                {FtpCommands.Options,new OptsCommand(this) },
                {FtpCommands.UserPassword,new PassCommand(this) },
                {FtpCommands.PassiveConnection,new PasvCommand(this) },
                {FtpCommands.DataChannelBufferSize,new PbszCommand(this)  },
                {FtpCommands.DataChannelProtection,new ProtCommand(this)  },
                {FtpCommands.PrintDirectory,new PwdCommand(this) },
                {FtpCommands.Quit,new QuitCommand(this) },
                {FtpCommands.DownloadFile,new RetrCommand(this, Logger)  },
                {FtpCommands.RemoveDirectory,new RmdCommand(this, Logger) },
                {FtpCommands.RenameFrom,new RnfrCommand(this, Logger)  },
                {FtpCommands.RenameTo,new RntoCommand(this) },
                {FtpCommands.SiteSpecific,new SiteCommand(this, Logger)},
                {FtpCommands.Size ,new SizeCommand(this, Logger)},
                {FtpCommands.UploadFile,new StorCommand(this, Logger) },
                {FtpCommands.SystemType,new SystCommand(this) },
                {FtpCommands.ChangeTransferType,new TypeCommand(this) },
                {FtpCommands.UserLogin, new UserCommand(this, Logger) },
                {"Unrecognized",new UnrecognizedCommand(this) }
            };
        }

        #endregion

        #region Dependencies
        private IAuthenticationProvider AuthenticationProvider { get; set; }
        public ICloudStorageFileSystemProvider FileSystemProvider { get; set; }
        private ILogger Logger { get; set; }
        private TcpClient ConnectedClient { get; }

        #endregion

        #region Fields
        public Stream ClientCommandStream { get; set; }

        public StreamReader CommandStreamReader { get; set; }

        public DataConnection ClientDataConnection { get; }
        /// <summary>
        /// Client can-reconnect or change port for some reason, 
        /// so to keep track of user, store the initial endpoint
        /// </summary>
        public IPEndPoint ClientInitialRemoteEndPoint { get; set; }

        //Active / passive
        public ConnectionType UserConnectionType { get; set; }

        public ControlConnectionFlags ConnectionFlags { get; set; }

        public Encoding ServerEncoding { get; set; } = Encoding.UTF8;

        public bool IsAuthenticated { get; set; }

        private Dictionary<string, FtpCommand> Commands { get; set; }

        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (ClientCommandStream is FtpSslStream)
                        (ClientCommandStream as FtpSslStream)?.Close();

                    ActionsTracker.UserDisconnected(null, ClientInitialRemoteEndPoint);
                    ConnectedClient.Close();
                }

                ClientCommandStream = null;
                FileSystemProvider = null;
                AuthenticationProvider = null;
                CommandStreamReader = null;

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

        #region Cancellation check

        private async Task CheckIfCancelled(CancellationToken token)
        {
            while (true)
            {
                if (token.IsCancellationRequested)
                {
                    Dispose();
                    return;
                }

                await Task.Delay(250);
            }
        }

        #endregion

        #region Main connection methods

        public async Task InitiateConnection(CancellationToken token)
        {
            SendResponse(new FtpReply() { ReplyCode = FtpReplyCode.ServiceReady, Message = "Service is ready." }, false);
            _ = Task.Run(() => CheckIfCancelled(token));
            while (true)
            {
                if (disposedValue)
                    return;

                await ExecuteCommand(await GetNewCommand());
            }
        }

        private async Task<string> GetNewCommand()
        {
                                        
            if ((CommandStreamReader != null) && (CommandStreamReader.CurrentEncoding != ServerEncoding))
                CommandStreamReader = new StreamReader(ClientCommandStream, ServerEncoding);

            while (CommandStreamReader != null)
            {
                var command = await CommandStreamReader.ReadLineAsync();
                //According to https://stackoverflow.com/questions/6958255/what-are-some-reasons-networkstream-read-would-hang-block
                //While connected, network stream blocks and waits for incoming commands.
                //if client disconnects, networkstream simply returns empty string, even though tcpClient's IsConnected might be "true"
                //To summarize, simply close the connection if command was null
                if (string.IsNullOrEmpty(command))
                {
                    Dispose();
                    return null;
                }

                Logger.Log($"{command}", RecordKind.CommandReceived);

                return command;
            }

            return null;
        }

        private async Task ExecuteCommand(string command)
        {
            if (disposedValue)
                return;

            if (string.IsNullOrWhiteSpace(command))
                return;

            var parameter = string.Empty;

            var index = command.IndexOf(" ", StringComparison.Ordinal);

            if (!index.Equals(-1))
            {
                parameter = command.Substring(index + 1);
                command = command.Substring(0, index);
            }

            //main logic of command execution

            if (!Commands.ContainsKey(command))
            {
                await Commands["Unrecognized"].Execute(parameter);
                return;
            }

            var result = await Commands[command].Execute(parameter);

            if (result == null)
                return;

            SendResponse(result, false);

            if (Commands[command] is QuitCommand)
            {
                Dispose();
            }
        }

        public void SendResponse(FtpReply ftpReply, bool IsRawReply)
        {
            if (disposedValue)
                return;

            var bytesMessage = ServerEncoding.GetBytes(ftpReply.Message);

            if (IsRawReply)
            {
                ClientCommandStream.Write(bytesMessage, 0, bytesMessage.Length);
                return;
            }

            var reply = ServerEncoding.GetBytes((int)ftpReply.ReplyCode + " " + ftpReply.Message + "\r\n");

            ClientCommandStream.Write(reply, 0, reply.Length);
        }

        #endregion

        #region Below are the Methods called by commands to operate with fields in this class && Connection-related
        /// <summary>
        /// Enables encryption of command channel.
        /// Usually called after AUTH command is sent
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public async Task OnEncryptionEnabled()
        {
            SendResponse(new FtpReply() { ReplyCode = FtpReplyCode.ServiceReady, Message = "Service is ready." }, false);

            ConnectionFlags |= ControlConnectionFlags.UsingTLSorSSL;

            var authStream = new FtpSslStream(ClientCommandStream);

            var certificate = new X509Certificate2(DefaultServerValues.CertificateLocation,
                                                    string.Empty);

            await authStream.AuthenticateAsServerAsync(certificate);

            ClientCommandStream = authStream;

            CommandStreamReader = new StreamReader(ClientCommandStream, ServerEncoding);

            ActionsTracker.ConnectionSecurityChanged(null, new ConnectionSecurityChangedEventArgs()
            {
                EndPoint = ClientInitialRemoteEndPoint,
                Security = ClientDataConnection.IsEncryptionActivated
                ? ConnectionSecurity.Both
                : ConnectionSecurity.ControlConnectionSecured
            });

            Logger.Log($"Successfully authenticated via TLS : {ClientInitialRemoteEndPoint.ToString()}"
                , RecordKind.Status);

        }

        public bool OnAuthenticateUser(string login, string pass)
        {
            return AuthenticationProvider.Authenticate(login, pass);
        }

        public async Task OnUserRegistered(string[] commandWords)
        {
            await DatabaseHelper.NewRecord(commandWords[1], commandWords[2]);
        }

        public void OnEnterActiveMode(string endPoint)
        {
            // Example of a command : PORT 127,0,0,1,203,175
            var endPointBytes = endPoint.Split(',');
            var portBytes = new byte[2] { byte.Parse(endPointBytes[4]), byte.Parse(endPointBytes[5]) };

            ClientDataConnection.InitializeActiveConnection(portBytes[0] * 256 + portBytes[1],
                new IPEndPoint(
                    IPAddress.Parse($"{endPointBytes[0]}.{endPointBytes[1]}.{endPointBytes[2]}.{endPointBytes[3]}")
                    , 0));

            UserConnectionType = ConnectionType.ACTIVE;
        }

        public int OnDataBufferSizeChanged(int size)
        {
            ClientDataConnection.BufferSize = size;
            return ClientDataConnection.BufferSize;
        }

        public string OnEnterPassiveMode()
        {
            var listeningPort = ClientDataConnection.InitializePassiveConnection();
            //send IP of server cuz by sending local ip client won't be able to connect
            var addressBytes = IPAddress.Parse(DefaultServerValues.ServerExternalIP).GetAddressBytes();

            UserConnectionType = ConnectionType.PASSIVE;

            return string.Format(
             "Entering Passive Mode ({0},{1},{2},{3},{4},{5})",
             addressBytes[0],
             addressBytes[1],
             addressBytes[2],
             addressBytes[3],
             (byte)(listeningPort / 256),
             listeningPort % 256);
        }

        public void OnQuit()
        {
            Dispose();
        }

        #endregion

        #region Download/receive

        public async Task OnUploadFile(string parameter)
        {
            await OpenDataConnection();
            await ClientDataConnection.ReceiveBytes(parameter);
            ClientDataConnection.Disconnect();
        }

        public async Task OnDownloadFile(string parameter)
        {
            var stream = FileSystemProvider.GetFileStream(parameter);
            await OnSendData(stream);
        }

        #endregion

        #region Directories

        public void OnSetWorkingDirectory(string parameter)
        {
            FileSystemProvider.WorkingDirectory = parameter;
        }

        public long OnGetOccupiedSpace(string path)
        {
            return FileSystemProvider.GetOccupiedDirectoryorFileSpace(path);
        }

        public void OnDelete(string parameter)
        {
            FileSystemProvider.Delete(parameter);
        }

        public void OnMoveUp()
        {
            FileSystemProvider.MoveUp();
        }

        public void OnCreateDirectory(string parameter)
        {
            FileSystemProvider.CreateDirectory(parameter);
        }

        #endregion

        #region Data channel - related
        public async Task OnSendData(Stream listingStream)
        {
            await OpenDataConnection().ConfigureAwait(false);
            await ClientDataConnection.SendBytes(listingStream);
            ClientDataConnection.Disconnect();
        }

        public async Task OnDataChannelEncryptionEnabled()
        {
            ClientDataConnection.ActivateEncryption();

            ActionsTracker.ConnectionSecurityChanged(null, new ConnectionSecurityChangedEventArgs()
            {
                EndPoint = ClientInitialRemoteEndPoint,
                Security = ConnectionFlags.HasFlag(ControlConnectionFlags.UsingTLSorSSL)
                ? ConnectionSecurity.Both
                : ConnectionSecurity.DataChannelSecured
            });

            Logger.Log($"Enabled encryption for datachannel : {ClientInitialRemoteEndPoint.ToString()}"
                , RecordKind.Status);
        }

        public async Task OnDataChannelEncryptionDisabled()
        {
            ClientDataConnection.DisableEncryption();

            ActionsTracker.ConnectionSecurityChanged(null, new ConnectionSecurityChangedEventArgs()
            {
                EndPoint = ClientInitialRemoteEndPoint,
                Security = ConnectionFlags.HasFlag(ControlConnectionFlags.UsingTLSorSSL)
                ? ConnectionSecurity.ControlConnectionSecured
                : ConnectionSecurity.NonSecure
            });
        }

        public async Task OnAuthenticated(string username)
        {
            IsAuthenticated = true;

            ActionsTracker.UserAuthenticated(null, new UserAuthenticatedEventArgs()
            {
                EndPoint = ClientInitialRemoteEndPoint,
                UserName = username
            });

            FileSystemProvider.Initialize(username);
        }


        private async Task OpenDataConnection()
        {
            if (ClientDataConnection != null && ClientDataConnection.IsConnectionOpen)
            {
                SendResponse(new FtpReply() { ReplyCode = FtpReplyCode.TransferStarting, Message = "Transfer is starting." }, false);
                return;
            }

            SendResponse(new FtpReply() { ReplyCode = FtpReplyCode.AboutToOpenDataConnection, Message = "Trying to open data connection." }, false);

            switch (UserConnectionType)
            {
                case ConnectionType.ACTIVE:
                    {
                        ClientDataConnection.OpenActiveConnection();
                        break;
                    }
                case ConnectionType.EXT_PASSIVE:
                case ConnectionType.PASSIVE:
                    {
                        ClientDataConnection.OpenPassiveConnection();
                        break;
                    }
            }
        }
        #endregion

        #region Other commands

        public void OnUtf8Enabled()
        {
             ServerEncoding = Encoding.UTF8;
            ConnectionFlags |= ControlConnectionFlags.UTF8ON;
        }

        public void OnUtf8Disabled()
        {
            ServerEncoding = Encoding.ASCII;
            ConnectionFlags &= ~ControlConnectionFlags.UTF8ON;
        }

        public async Task<string> OnGetFileLastModified(string ftpPath)
        {
            return FileSystemProvider.GetFileLastModifiedTime(ftpPath);
        }

        public void OnSendFeatureList(FtpReply list)
        {
            SendResponse(list, true);
        }

        public async Task<string> OnRenameFromCommandReceived()
        {
            SendResponse(new FtpReply() { ReplyCode = FtpReplyCode.FileActionPendingInfo, Message = "Waiting for RNTO command." }, false);

            return await GetNewCommand();
        }

        public void OnRename(string renameFrom, string renameTo)
        {
            FileSystemProvider.Rename(renameFrom, renameTo);
        }


        /// <summary>
        /// UserCommand class calls this method
        /// to get new command, which should be PASS
        /// </summary>
        /// <param name="username"></param>
        /// <returns>Next received command</returns>

        public async Task<string> OnUserCommandReceived(string username)
        {
            IsAuthenticated = false;

            if (string.IsNullOrEmpty(username))
            {
                SendResponse(new FtpReply() { ReplyCode = FtpReplyCode.BadSequence, Message = "No user login was provided." }, false);
                return null;
            }

            SendResponse(new FtpReply() { ReplyCode = FtpReplyCode.NeedPassword, Message = "Waiting for password." }, false);

            return await GetNewCommand();
        }


        #endregion

    }
}