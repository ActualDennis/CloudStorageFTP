using CloudStorage.Server.Authentication;
using CloudStorage.Server.Commands;
using CloudStorage.Server.Connections;
using CloudStorage.Server.Data;
using CloudStorage.Server.Di;
using CloudStorage.Server.Factories;
using CloudStorage.Server.FileSystem;
using CloudStorage.Server.Helpers;
using CloudStorage.Server.Logging;
using CloudStorage.Server.Misc;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CloudStorage.Server {
    /// <summary>
    /// Class used to accept commands from ftp client and execute them.
    /// </summary>
    public class ControlConnection : IDisposable {
        #region Constructor
        public ControlConnection(
            IAuthenticationProvider authenticationProvider,
            ILogger logger,
            ICloudStorageFileSystemProvider FileSystemProvider,
            DataConnection dataConnection,
            FtpCommandFactory commandsFactory,
            DatabaseHelper DbHelper)
        {
            ClientDataConnection = dataConnection;
            AuthenticationProvider = authenticationProvider;
            Logger = logger;
            this.FileSystemProvider = FileSystemProvider;
            ftpCommandFactory = commandsFactory;
            ClientDataConnection.Initialize(this.FileSystemProvider);
            this.DbHelper = DbHelper;
        }

        #endregion

        #region Dependencies
        private IAuthenticationProvider AuthenticationProvider { get; set; }
        public  ICloudStorageFileSystemProvider FileSystemProvider { get; set; }
        private ILogger Logger { get; set; }
        private TcpClient ConnectedClient { get; set; }
        private FtpCommandFactory ftpCommandFactory { get; }
        private DatabaseHelper DbHelper { get; }

        private bool IsEncryptionSupported { get; set; }

        #endregion

        #region Fields
        private Stream ClientCommandStream { get; set; }

        private StreamReader CommandStreamReader { get; set; }

        public DataConnection ClientDataConnection { get; private set; }
        /// <summary>
        /// Client can-reconnect or change port for some reason, 
        /// so to keep track of user, store the initial endpoint
        /// </summary>
        public IPEndPoint ClientInitialRemoteEndPoint { get; set; }

        //Active / passive
        private ConnectionType UserConnectionType { get; set; }

        private ControlConnectionFlags ConnectionFlags { get; set; }

        private Encoding ServerEncoding { get; set; } = Encoding.UTF8;

        public bool IsAuthenticated { get; set; }

        #endregion

        #region Initialization
        public void Initialize(TcpClient client, bool IsEncryptionEnabled)
        {
            ConnectedClient = client;
            ClientCommandStream = client.GetStream();
            CommandStreamReader = new StreamReader(ClientCommandStream, ServerEncoding);
            ClientInitialRemoteEndPoint = (IPEndPoint)client.Client.RemoteEndPoint;
            IsEncryptionSupported = IsEncryptionEnabled;
        }

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

        #region Main connection methods

        public async Task InitiateConnection()
        {
            SendResponse(new FtpReply() { ReplyCode = FtpReplyCode.ServiceReady, Message = "Service is ready." }, false);

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

                //Close the connection if command was null
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

            var ftpCommand = ftpCommandFactory.GetCommand(command, this, Logger);

            var result = await ftpCommand.Execute(parameter);

            if (result == null)
                return;

            SendResponse(result, false);

            if (ftpCommand is QuitCommand)
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
            if (!IsEncryptionSupported)
            {
                SendResponse(new FtpReply() { ReplyCode = FtpReplyCode.NotImplemented, Message = "Server is not configured to support SSL/TLS." }, false);
                return;
            }

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
            await DbHelper.NewRecord(commandWords[1], commandWords[2]);
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
            if (!IsEncryptionSupported)
            {
                SendResponse(new FtpReply() { ReplyCode = FtpReplyCode.NotImplemented, Message = "Server is not configured to support SSL/TLS." }, false);
                return;
            }

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