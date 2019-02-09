using CloudStorage.Server.Authentication;
using CloudStorage.Server.Connections;
using CloudStorage.Server.Data;
using CloudStorage.Server.Exceptions;
using CloudStorage.Server.FileSystem;
using CloudStorage.Server.Helpers;
using CloudStorage.Server.Logging;
using CloudStorage.Server.Misc;
using System;
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
    public class ControlConnection : IDisposable
    {
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
        }

        #endregion

        #region Dependencies

        private IAuthenticationProvider AuthenticationProvider { get; set; }
        private ICloudStorageFileSystemProvider FileSystemProvider { get; set; }
        private ILogger Logger { get; set; }
        private TcpClient ConnectedClient { get; }

        #endregion

        #region Private Fields
        private Stream ClientCommandStream { get; set; }

        private StreamReader CommandStreamReader { get; set; }

        private DataConnection ClientDataConnection { get; }
        /// <summary>
        /// Client can-reconnect or change port for some reason, 
        /// so to keep track of user, store the initial endpoint
        /// </summary>
        private IPEndPoint ClientInitialRemoteEndPoint { get; set; }

        //Active / passive
        private ConnectionType UserConnectionType { get; set; }

        public ControlConnectionFlags ConnectionFlags { get; private set; }

        private Encoding ServerEncoding { get; set; } = Encoding.UTF8;

        private bool IsAuthenticated { get; set; }

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

        #region User input checks

        private async Task CheckUserInput(string parameter, bool checkForParameterNeeded)
        {
            if (!IsAuthenticated)
            {
                await SendResponse(ReplyCode.NotLoggedIn, "Log in with USER command first.");
                return;
            }

            if (!checkForParameterNeeded)
                return;

            if (string.IsNullOrWhiteSpace(parameter))
                await SendResponse(ReplyCode.SyntaxErrorInParametersOrArguments, "No parameter was provided.");
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
            await SendResponse(ReplyCode.ServiceReady, "Service is ready.");
            _ = Task.Run(() => CheckIfCancelled(token));
            while (true)
            {
                if (disposedValue)
                    return;

                try
                {
                    await ExecuteCommand(await GetNewCommand());
                }
                catch (MsDosPathNotSupportedException ex)
                {
                    Logger.Log($"{ex.Message}", RecordKind.Error);
                    await SendResponse(ReplyCode.NotImplemented, ex.Message);
                }
                catch (Exception ex)
                {
                    if (ex is ObjectDisposedException)
                        return;

            
                    if(ex is UserOutOfSpaceException)
                    {
                        await SendResponse(ReplyCode.FileSpaceInsufficient, $"Your storage is full. Try deleting files to free it.: {ex.Message}");
                        return;
                    }

                    Logger.Log(ex.Message, RecordKind.Error);

                    if ((ex is FormatException)
                    || (ex is InvalidOperationException)
                    || (ex is DirectoryNotFoundException)
                    || (ex is FileNotFoundException))
                    {
                        await SendResponse(ReplyCode.FileNoAccess, ex.Message);
                        return;
                    }

                    if ((ex is UnauthorizedAccessException)
                      || (ex is IOException))
                        await SendResponse(ReplyCode.FileBusy, ex.Message);

                    await SendResponse(ReplyCode.LocalError, $"Error happened: {ex.Message}");
                }
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

        private async Task SendResponse(ReplyCode replyCode, string message)
        {
            if (disposedValue)
                return;

            var reply = ServerEncoding.GetBytes((int)replyCode + " " + message + "\r\n");

            await ClientCommandStream.WriteAsync(reply, 0, reply.Length);
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

            switch (command)
            {
                case FtpCommands.ActiveConnection:
                    {
                        await CheckUserInput(parameter, true);
                        EnterActiveMode(parameter);
                        await SendResponse(ReplyCode.Okay, "Ready to communicate.");
                        break;
                    }
                case FtpCommands.PassiveConnection:
                    {
                        await CheckUserInput(parameter, false);
                        await EnterPassiveMode();
                        break;
                    }
                case FtpCommands.ExtendedPassiveConnection:
                    {
                        await CheckUserInput(parameter, false);
                        await EnterExtendedPassiveMode();
                        break;
                    }
                case FtpCommands.ClientInfo:
                    {
                        Logger.Log($"{ClientInitialRemoteEndPoint.ToString()} connected via {parameter}.", RecordKind.Status);
                        await SendResponse(ReplyCode.Okay, $"Accepted information about client.");
                        break;
                    }
                case FtpCommands.EnableEncryption:
                    {
                        await TryEnableEncryption(parameter);
                        break;
                    }
                case FtpCommands.DataChannelBufferSize:
                    {
                        await CheckUserInput(parameter, false);
                        int.TryParse(parameter, out var result);
                        ClientDataConnection.BufferSize = result;
                        await SendResponse(ReplyCode.Okay, $"Buffer size was set to {ClientDataConnection.BufferSize}.");
                        break;
                    }
                case FtpCommands.DataChannelProtection:
                    {
                        await CheckUserInput(parameter, true);

                        switch (parameter)
                        {
                            case "P":
                                {
                                    ClientDataConnection.ActivateEncryption();
                                    await SendResponse(ReplyCode.Okay,
                                        "Successfully enabled encryption. You can now open data connection.");

                                    ActionsTracker.ConnectionSecurityChanged(null, new ConnectionSecurityChangedEventArgs()
                                    {
                                        EndPoint = ClientInitialRemoteEndPoint,
                                        Security = ConnectionFlags.HasFlag(ControlConnectionFlags.UsingTLSorSSL)
                                        ? ConnectionSecurity.Both
                                        : ConnectionSecurity.DataChannelSecured
                                    });

                                    Logger.Log($"Enabled encryption for datachannel : {ClientInitialRemoteEndPoint.ToString()}"
                                        , RecordKind.Status);

                                    break;

                                }
                            case "C":
                                {
                                    ClientDataConnection.DisableEncryption();

                                    await SendResponse(ReplyCode.Okay,
                                    "Warning! Now using plain unencrypted way of data transmission. Consider using PROT P instead.");

                                    ActionsTracker.ConnectionSecurityChanged(null, new ConnectionSecurityChangedEventArgs()
                                    {
                                        EndPoint = ClientInitialRemoteEndPoint,
                                        Security = ConnectionFlags.HasFlag(ControlConnectionFlags.UsingTLSorSSL)
                                        ? ConnectionSecurity.ControlConnectionSecured
                                        : ConnectionSecurity.NonSecure
                                    });

                                    break;
                                }
                            default:
                                {
                                    await SendResponse(ReplyCode.NotImplemented, "Use either of two: PROT P or PROT C.");
                                    break;
                                }
                        }

                        break;
                    }
                case FtpCommands.UserLogin:
                    {
                        await TryLogInUser(parameter);
                        break;
                    }
                case FtpCommands.UserPassword:
                    {
                        await SendResponse(ReplyCode.SyntaxErrorInParametersOrArguments, "Use USER command followed by PASS command.");
                        break;
                    }
                case FtpCommands.SystemType:
                    {
                        await SendResponse(ReplyCode.SystemTypeName, "UNIX Type: L8");
                        break;
                    }
                case FtpCommands.FeatureList:
                    {
                        await ReplyFeatureList(ReplyCode.SystemStatus, "Features: \n\rUTF8 \n\rTVFS \n\rSIZE \n\rMLST Type*;Size*;Perm*;Modify*;");
                        break;
                    }
                case FtpCommands.PrintDirectory:
                    {
                        if (!IsAuthenticated)
                        {
                            await SendResponse(ReplyCode.NotLoggedIn, "Log in with USER command first.");
                            return;
                        }

                        await SendResponse(ReplyCode.PathCreated, $"\"{FileSystemProvider.WorkingDirectory}\"");
                        break;
                    }
                case FtpCommands.ChangeTransferType:
                    {
                        switch (parameter)
                        {
                            case "A":
                                await SendResponse(ReplyCode.Okay, "Now using ascii type for transferring data.");
                                return;
                            case "I":
                                await SendResponse(ReplyCode.Okay, "Now using binary type for transferring data.");
                                return;
                            default:
                                await SendResponse(ReplyCode.ParameterNotImplemented, "Unknown type");
                                return;
                        }
                    }
                case FtpCommands.TransmissionMode:
                    {
                        switch (parameter)
                        {
                            case "S":
                                await SendResponse(ReplyCode.Okay, "Using stream mode");
                                return;
                            default:
                                await SendResponse(ReplyCode.ParameterNotImplemented, "Not implemented yet.");
                                return;
                        }
                    }

                case FtpCommands.DirectoryListing:
                    {
                        await CheckUserInput(parameter, false);
                        await GetDirectoryListing();
                        break;
                    }
                case FtpCommands.ExtendedDirectoryListing:
                    {
                        await CheckUserInput(parameter, false);
                        await GetExtendedDirectoryListing(parameter);
                        break;
                    }
                case FtpCommands.ChangeWorkingDirectory:
                    {
                        await CheckUserInput(parameter, true);
                        FileSystemProvider.WorkingDirectory = parameter;
                        await SendResponse(ReplyCode.Okay, $"Changed directory to {parameter}");

                        break;
                    }
                case FtpCommands.CreateDirectory:
                    {
                        await CheckUserInput(parameter, true);

                        FileSystemProvider.CreateDirectory(parameter);

                        await SendResponse(ReplyCode.PathCreated, "Successfully created path.");

                        break;
                    }

                case FtpCommands.RenameFrom:
                    {
                        await TryRename(parameter);
                        break;
                    }

                case FtpCommands.RenameTo:
                    {
                        await SendResponse(ReplyCode.BadSequence, "Use RNFR followed by RNTO.");
                        break;
                    }
                case FtpCommands.RemoveDirectory:
                    {
                        await CheckUserInput(parameter, true);
                        FileSystemProvider.Delete(parameter);
                        await SendResponse(ReplyCode.FileActionOk, $"Successfully deleted the directory {parameter}.");
                        break;
                    }
                case FtpCommands.GoUp:
                    {
                        await CheckUserInput(parameter, false);
                        FileSystemProvider.MoveUp();
                        await SendResponse(ReplyCode.Okay, "Successfully moved up.");
                        break;
                    }
                case FtpCommands.DownloadFile:
                    {
                        await CheckUserInput(parameter, true);
                        var stream = FileSystemProvider.GetFileStream(parameter);
                        await OpenDataConnection();
                        await ClientDataConnection.SendBytes(stream);
                        ClientDataConnection.Disconnect();
                        await SendResponse(ReplyCode.SuccessClosingDataConnection, "Transfer complete.");
                        break;
                    }
                case FtpCommands.UploadFile:
                    {
                        await CheckUserInput(parameter, true);
                        await OpenDataConnection();
                        await ClientDataConnection.ReceiveBytes(parameter);
                        ClientDataConnection.Disconnect();
                        await SendResponse(ReplyCode.SuccessClosingDataConnection, "Transfer complete.");
                        break;
                    }
                case FtpCommands.SiteSpecific:
                    {
                        if (string.IsNullOrEmpty(parameter))
                        {
                            await SendResponse(ReplyCode.SyntaxErrorInParametersOrArguments, "No parameter was provided.");
                            return;
                        }

                        var commandWords = parameter.Split(' ');

                        switch (commandWords[0])
                        { //To register , user should log in as anonymous and send example command:
                          //SITE REG *username* *password*
                            case LocalFtpCommands.Register:
                                {
                                    if (commandWords.Length < 2 + 1)
                                    {
                                        await SendResponse(ReplyCode.SyntaxErrorInParametersOrArguments,
                                            "Example usage: SITE REG *username* *password*.");
                                        return;
                                    }

                                    await DatabaseHelper.NewRecord(commandWords[1], commandWords[2]);
                                    await SendResponse(ReplyCode.Okay, $"Successfully registered.");
                                    break;
                                }
                        }

                        break;
                    }
                case FtpCommands.DeleteFile:
                    {
                        await CheckUserInput(parameter, true).ConfigureAwait(false);

                        FileSystemProvider.Delete(parameter);

                        await SendResponse(ReplyCode.FileActionOk, $"Successfully deleted {parameter}");
                        break;
                    }
                case FtpCommands.Size:
                    {
                        await CheckUserInput(parameter, true).ConfigureAwait(false);
                        var size = FileSystemProvider.GetOccupiedDirectoryorFileSpace(parameter);
                        await SendResponse(ReplyCode.FileStatus, $"{size}");
                        break;
                    }
                case FtpCommands.NameListing:
                    {
                        await CheckUserInput(parameter, false).ConfigureAwait(false);
                        await GetDirectoryNamesListing();
                        break;

                    }
                case FtpCommands.FileLastModifiedTime:
                    {
                        await GetFileLastModified(parameter);
                        break;
                    }
                case FtpCommands.FileorDirectoryInfo:
                    {
                        await GetFileorDirectoryInfo(parameter);
                        break;
                    }
                case FtpCommands.Options:
                    {
                        if (string.IsNullOrEmpty(parameter))
                        {
                            await SendResponse(ReplyCode.SyntaxErrorInParametersOrArguments, "No parameter was provided.");
                            return;
                        }

                        if (!parameter.IndexOf("UTF8").Equals(-1))
                        {
                            if (parameter.IndexOf(" ").Equals(-1))
                            {
                                await SendResponse(ReplyCode.SyntaxErrorInParametersOrArguments,
                                    "Use ON/OFF keywords to enable/disable UTF8.");
                                return;
                            }

                            var secondaryCommand = parameter.Substring(parameter.IndexOf(" ") + 1);

                            switch (secondaryCommand)
                            {
                                case "ON":
                                    {
                                        ServerEncoding = Encoding.UTF8;
                                        ConnectionFlags |= ControlConnectionFlags.UTF8ON;
                                        await SendResponse(ReplyCode.Okay, "Now using UTF8 encoding.");
                                        break;
                                    }
                                case "OFF":
                                    {
                                        ServerEncoding = Encoding.ASCII;
                                        ConnectionFlags &= ~ControlConnectionFlags.UTF8ON;
                                        await SendResponse(ReplyCode.Okay, "Now using ASCII encoding.");
                                        break;
                                    }
                                default:
                                    {
                                        await SendResponse(ReplyCode.SyntaxErrorInParametersOrArguments,
                                            "Use ON/OFF keywords to enable/disable UTF8.");
                                        return;
                                    }
                            }
                        }

                        break;
                    }
                case FtpCommands.KeepAlive:
                    {
                        await SendResponse(ReplyCode.Okay, "Connection is ok.");
                        break;
                    }
                case FtpCommands.Quit:
                    {
                        await CheckUserInput(parameter, false).ConfigureAwait(false);
                        await SendResponse(ReplyCode.SuccessClosingDataConnection, "Successfully closed data connection.");
                        Dispose();
                        break;
                    }
                default:
                    {
                        await SendResponse(ReplyCode.SyntaxErrorInParametersOrArguments, "Command was not recognized.");
                        break;
                    }
            }
        }

        #endregion

        #region Connection-related
        /// <summary>
        /// Enables encryption of command channel.
        /// Usually called after AUTH command is sent
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        private async Task TryEnableEncryption(string parameter)
        {
            if (string.IsNullOrEmpty(parameter))
            {
                await SendResponse(ReplyCode.BadSequence, "No parameter was provided.");
                return;
            }

            switch (parameter)
            {
                case "SSL":
                case "TLS":
                    {
                        await SendResponse(ReplyCode.ServiceReady, "Service is ready.");

                        ConnectionFlags |= ControlConnectionFlags.UsingTLSorSSL;

                        var authStream = new FtpSslStream(ClientCommandStream);

                        var certificate = new X509Certificate2(DefaultServerValues.CertificateLocation,
                                                               string.Empty);

                        authStream.AuthenticateAsServer(certificate);

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

                        break;
                    }
            }
        }

        private async Task OpenDataConnection()
        {
            if (ClientDataConnection != null && ClientDataConnection.IsConnectionOpen)
            {
                await SendResponse(ReplyCode.TransferStarting, "Transfer is starting.");
                return;
            }

            await SendResponse(ReplyCode.AboutToOpenDataConnection, "Trying to open data connection.");

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

        private void EnterActiveMode(string endPoint)
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

        private async Task EnterPassiveMode()
        {
            var listeningPort = ClientDataConnection.InitializePassiveConnection();
            //send IP of server cuz by sending local ip client won't be able to connect
            var addressBytes = IPAddress.Parse(DefaultServerValues.ServerExternalIP).GetAddressBytes();

            await SendResponse(ReplyCode.EnteringPassiveMode, string.Format(
                "Entering Passive Mode ({0},{1},{2},{3},{4},{5})",
                addressBytes[0],
                addressBytes[1],
                addressBytes[2],
                addressBytes[3],
                (byte)(listeningPort / 256),
                listeningPort % 256)
            );

            UserConnectionType = ConnectionType.PASSIVE;
        }

        private async Task EnterExtendedPassiveMode()
        {
            var listeningPort = ClientDataConnection.InitializePassiveConnection();

            await SendResponse(ReplyCode.EnteringExtendedPassiveMode,
                $"Entering Extended Passive Mode (|||{listeningPort.ToString()}|)");
        }

        #endregion

        #region Other commands

        private async Task GetFileLastModified(string ftpPath)
        {
            await CheckUserInput(ftpPath, true).ConfigureAwait(false);
            var lastModified = FileSystemProvider.GetFileLastModifiedTime(ftpPath);
            await SendResponse(ReplyCode.FileStatus, $" {lastModified}\r\n");
        }

        private async Task TryRename(string renameFrom)
        {
            await CheckUserInput(renameFrom, true);
            await SendResponse(ReplyCode.FileActionPendingInfo, "Waiting for RNTO command.");

            var newCommand = await GetNewCommand();
            string newParam;
            var spaceIndex = newCommand.IndexOf(" ", StringComparison.Ordinal);

            if (spaceIndex.Equals(-1))
            {
                await SendResponse(ReplyCode.SyntaxErrorInParametersOrArguments, "No parameter for PASS was provided.");
                return;
            }

            newParam = newCommand.Substring(spaceIndex + 1);
            newCommand = newCommand.Substring(0, spaceIndex);

            if (newCommand != FtpCommands.RenameTo)
            {
                await SendResponse(ReplyCode.SyntaxErrorInParametersOrArguments, "RNFR is always followed by RNTO.");
                return;
            }

            FileSystemProvider.Rename(renameFrom, newParam);
            await SendResponse(ReplyCode.FileActionOk, "Successfully renamed.");

        }

        private async Task TryLogInUser(string username)
        {
            IsAuthenticated = false;

            if (string.IsNullOrEmpty(username))
            {
                await SendResponse(ReplyCode.BadSequence, "No user login was provided.");
                return;
            }

            await SendResponse(ReplyCode.NeedPassword, "Waiting for password.");

            var newCommand = await GetNewCommand();
            var spaceIndex = newCommand.IndexOf(" ", StringComparison.Ordinal);
            string password;

            if (spaceIndex.Equals(-1))
            {
                await SendResponse(ReplyCode.SyntaxErrorInParametersOrArguments, "No parameter for PASS was provided.");
                return;
            }

            password = newCommand.Substring(spaceIndex + 1);
            newCommand = newCommand.Substring(0, spaceIndex);

            if (newCommand != FtpCommands.UserPassword)
            {
                await SendResponse(ReplyCode.SyntaxErrorInParametersOrArguments, "USER is always followed by PASS command.");
                return;
            }

            if (!AuthenticationProvider.Authenticate(username, password))
            {
                await SendResponse(ReplyCode.NotLoggedIn, "Wrong password.");
                return;
            }

            IsAuthenticated = true;
            await SendResponse(ReplyCode.UserLoggedIn, "Successfully logged in.");

            ActionsTracker.UserAuthenticated(null, new UserAuthenticatedEventArgs()
            {
                EndPoint = ClientInitialRemoteEndPoint,
                UserName = username
            });

            FileSystemProvider.Initialize(username);
        }


        private async Task ReplyFeatureList(ReplyCode code, string message)
        {
            //Every line of reply should have leading space
            message = message.Replace("\r", " ");

            //this is required by specification
            message = message.Replace("\n", "\r\n");

            var replyString = $"{((int)code).ToString()}-{message}\r\n{((int)code).ToString()} End\r\n";

            var reply = ServerEncoding.GetBytes(replyString);

            await ClientCommandStream.WriteAsync(reply, 0, reply.Length);
        }

        #endregion

        #region Directory listings
        private async Task GetDirectoryNamesListing()
        {
            var entries = FileSystemProvider.EnumerateDirectory(null);

            var memStream = new MemoryStream();

            var writer = new StreamWriter(memStream);

            foreach (var entry in entries)
            {
                if(entry.EntryType == FileSystemEntryType.FILE)
                    await writer.WriteLineAsync(entry.Name);
            }

            writer.Flush();
            memStream.Seek(0, SeekOrigin.Begin);
            await OpenDataConnection().ConfigureAwait(false);
            await ClientDataConnection.SendBytes(memStream);
            ClientDataConnection.Disconnect();
            await SendResponse(ReplyCode.SuccessClosingDataConnection, "Successfully sent file system listing.");
            memStream.Close();
        }


        private async Task GetDirectoryListing()
        {
            var entries = FileSystemProvider.EnumerateDirectory(null);

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
            await OpenDataConnection().ConfigureAwait(false);
            await ClientDataConnection.SendBytes(memStream);
            ClientDataConnection.Disconnect();
            await SendResponse(ReplyCode.SuccessClosingDataConnection, "Successfully sent file system listing.");
            memStream.Close();
        }

        private async Task GetExtendedDirectoryListing(string parameter)
        {
            var entries = FileSystemProvider.EnumerateDirectory(parameter);
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
            await OpenDataConnection().ConfigureAwait(false);
            await ClientDataConnection.SendBytes(memStream);
            ClientDataConnection.Disconnect();
            await SendResponse(ReplyCode.SuccessClosingDataConnection, "Successfully sent file system listing.");
            memStream.Close();

        }

        private async Task GetFileorDirectoryInfo(string path)
        {
            var entries = FileSystemProvider.EnumerateDirectory(path);
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
            await OpenDataConnection();
            await ClientDataConnection.SendBytes(memStream);
            ClientDataConnection.Disconnect();
            await SendResponse(ReplyCode.SuccessClosingDataConnection, "Successfully sent file system listing.");
            memStream.Close();

        }

        #endregion

    }
}