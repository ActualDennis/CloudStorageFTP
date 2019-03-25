using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using CloudStorage.Server.Data;
using CloudStorage.Server.FileSystem;
using CloudStorage.Server.Logging;

namespace CloudStorage.Server.Connections
{
    /// <summary>
    /// Data connection should be created for each user 
    /// individually to transfer listings or files.
    /// </summary>
    public class DataConnection
    {
        public DataConnection(
            ILogger logger)
        {
            this.logger = logger;
        }

        public void Initialize(ICloudStorageFileSystemProvider fileSystemProvider)
        {
            FileSystemProvider = fileSystemProvider;
        }

        private const int MinDataConnections = 10;

        private static int _minPort;

        private static int _maxPort;

        public static int MinPort
        {
            get => _minPort;
            set
            {
                if (MaxPort == 0)
                    throw new InvalidOperationException("Set MaxPort first.");

                if ((value < MaxPort) && (MaxPort - value >= MinDataConnections))
                    _minPort = value;
            }
        }

        public static int MaxPort
        {
            get => _maxPort;
            set
            {
                if((value > 65535) || (value < 0 + MinDataConnections))
                    throw new InvalidOperationException("Wrong port range.");

                _maxPort = value;
            }
        }


        private int _bufferSize = DefaultServerValues.MaxRecommendedBufferSize;

        private TcpClient _mainConnection;

        public int ListeningPort { get; private set; }

        private TcpClient MainConnection
        {
            get => _mainConnection;
            set
            {
                if (_mainConnection == null)
                {
                    _mainConnection = value;
                    return;
                }

                if (_mainConnection.Connected) return;
                _mainConnection = value;
            }
        }
        /// <summary>
        /// Socket that listens for incoming connections.
        /// Used by PASV command
        /// </summary>
        private TcpListener PassiveListener { get; set; }

        private IPEndPoint ActiveConnectionEndPoint { get; set; }

        /// <summary>
        /// Either secure or not data stream used to transfer listings and files.
        /// </summary>
        private Stream DataConnectionStream { get; set; }

        private ILogger logger { get; set; }

        private ICloudStorageFileSystemProvider FileSystemProvider { get; set; }
 
        public int BufferSize
        {
            get => _bufferSize;
            set
            {
                if (value == _bufferSize)
                    return;

                if (value == 0)
                {
                    _bufferSize = DefaultServerValues.MaxRecommendedBufferSize;
                    return;
                }

                if ((value >= DefaultServerValues.MinRecommendedBufferSize) &&
                    (value <= DefaultServerValues.MaxRecommendedBufferSize))
                    _bufferSize = value;
            }
        }

        public bool IsConnectionOpen => MainConnection != null && MainConnection.Connected;

        public bool IsEncryptionActivated { get; private set; }
         
        /// <summary>
        /// Sets IsEncryptionActivated flag to true
        /// Should call after command PROT P
        /// </summary>
        public void ActivateEncryption()
        {
            IsEncryptionActivated = true;
        }
        /// <summary>
        /// Sets IsEncryptionActivated flag to false
        /// Should call after command PROT C
        /// </summary>
        public void DisableEncryption()
        {
            IsEncryptionActivated = false;
        }
        /// <summary>
        /// Finds free port and remembers it, 
        /// to start listening on it after opening the connection
        /// </summary>
        /// <returns>Port it'll listen on</returns>
        public int InitializePassiveConnection()
        {
            //if connection was already opened, reply address we are listening on.
            if (ListeningPort != 0)
                return ListeningPort;

            // find free port
            var port = MinPort;
            for (; port < MaxPort; ++port)
            {
                PassiveListener = new TcpListener(IPAddress.Any, port);
                try
                {
                    PassiveListener.Start();
                    break;
                }
                catch (SocketException)
                {
                    logger.Log($"Port {port} is occupied.", RecordKind.Status);
                }
            }

            if (port == MaxPort)
            {
                ListeningPort = 0;
                throw new SystemException("All ports are occupied. Try again later.");
            }

            ListeningPort = port;

            logger.Log($"Listening on :{port}", RecordKind.Status);

            return ListeningPort;
        }
        /// <summary>
        /// Remembers endpoint to the client.
        /// </summary>
        /// <param name="port"></param>
        /// <param name="clientEndPoint"></param>
        public void InitializeActiveConnection(int port, IPEndPoint clientEndPoint)
        {
            ActiveConnectionEndPoint = new IPEndPoint(clientEndPoint.Address, port);
            ListeningPort = 0;
        }

        public void OpenActiveConnection()
        {
            //Use IpV4, probably will add ipv6 here
            MainConnection = new TcpClient(AddressFamily.InterNetwork);
            MainConnection.Connect(ActiveConnectionEndPoint);

            DataConnectionStream = IsEncryptionActivated
                ? CreatePrivateStream(MainConnection.GetStream())
                : MainConnection.GetStream();

            logger.Log($"Successfully connected to {ActiveConnectionEndPoint.ToString()} via ACTIVE method", RecordKind.Status);
        }

        public void OpenPassiveConnection()
        {
            if (ListeningPort == 0)
            {
                logger.Log($"Client tried to open connection without initializing it.", RecordKind.Error);
                return;
            }

            MainConnection = PassiveListener.AcceptTcpClient();

            DataConnectionStream = IsEncryptionActivated
                ? CreatePrivateStream(MainConnection.GetStream())
                : MainConnection.GetStream();

            logger.Log($"Successfully accepted PASV connection : {((IPEndPoint)MainConnection.Client.RemoteEndPoint).ToString()}", RecordKind.Status);
        }

        private Stream CreatePrivateStream(Stream clientStream)
        {
            var authStream = new FtpSslStream(clientStream);

            var certificate = new X509Certificate2(DefaultServerValues.CertificateLocation,
                                                   string.Empty);

            authStream.AuthenticateAsServer(certificate);

            logger.Log($"Successfully authenticated via TLS on data channel. : {((IPEndPoint)MainConnection.Client.RemoteEndPoint).ToString()}", RecordKind.Status);

            return authStream;
        }

        public async Task SendBytes(Stream source)
        {
            source.CopyTo(DataConnectionStream, _bufferSize);
            await DataConnectionStream.FlushAsync();
            logger.Log($"Successfully SENT a total of {source.Length} bytes. : {((IPEndPoint)MainConnection.Client.RemoteEndPoint).ToString()}", RecordKind.Status);
        }

        public async Task ReceiveBytes(string destination)
        {
            var destStream = FileSystemProvider.CreateNewFileorOverwrite(destination);
            await DataConnectionStream.CopyToAsync(destStream, _bufferSize);
            logger.Log($"Successfully RECEIVED a total of {destStream.Length} bytes. : {((IPEndPoint)MainConnection.Client.RemoteEndPoint).ToString()}", RecordKind.Status);
            destStream.Close();
        }

        public void Disconnect()
        {
            if (IsEncryptionActivated)
                ((FtpSslStream)DataConnectionStream).Close();

            MainConnection.Close();
            PassiveListener.Stop();
            PassiveListener = null;
            ListeningPort = 0;
            MainConnection = null;
        }
    }
}