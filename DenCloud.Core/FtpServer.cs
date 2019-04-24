using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using DenCloud.Core.Authentication;
using DenCloud.Core.Connections;
using DenCloud.Core.Data;
using DenCloud.Core.Di;
using DenCloud.Core.Factories;
using DenCloud.Core.FileSystem;
using DenCloud.Core.Helpers;
using DenCloud.Core.Logging;
using DenCloud.Core.Misc;

namespace DenCloud.Core {

    public class FtpServer : IDisposable
    {
        public FtpServer(
            ILogger logger)
        {
            this.logger = logger;
        }

        private TcpListener ConnectionsListener { get; set; }

        private Dictionary<Task, CancellationTokenSource> connections { get; set; } = new Dictionary<Task, CancellationTokenSource>();

        private ILogger logger { get; set; }

        private void Initialize()
        {
            try
            {
                var settings = XmlConfigHelper.ParseSettings();

                //For server to operate as you want , 
                //you must set desired values in Configuration.xml
                //If you are using ssl certificate, it should be without password for now.
                DefaultServerValues.CertificateLocation = settings.CertificateLocation;
                DefaultServerValues.BaseDirectory = settings.BaseDirectory;
                DefaultServerValues.FtpControlPort = settings.FtpControlPort;
                DefaultServerValues.ServerExternalIP = settings.ServerExternalIP;
                DefaultServerValues.LoggingPath = settings.LoggingPath;
                DataConnection.MaxPort = settings.MaxPort;
                DataConnection.MinPort = settings.MinPort;
                DataConnection.PassiveConnectionRetryFor = settings.PassiveConnectionRetryFor;
                //if this throws, base dir is basically a set of characters.

               DiContainer.Provider.Resolve<IFtpFileSystemProvider<FileSystemEntry>>();

            }
            catch(Exception ex)
            {
                logger.Log("There was a problem with config file. Check base directory / ip / ports and try again.", RecordKind.Error);
                throw new ApplicationException($"There was a problem with config file: {ex.Message}");
            }
        }

        public void Dispose()
        {
            ConnectionsListener.Stop();
            connections = null;
        }
        /// <summary>
        /// Call this method if you want to start listening for incoming requests
        /// and allow users to manage their virtual storage
        /// </summary>
        /// <returns></returns>
        public async Task Start(bool IsEncryptionEnabled)
        {
            try
            {
                DiContainer.ValidateProvider();
                Initialize();
            }
            catch (Exception ex)
            {
                logger.Log($"Server didn't start. Detailed error: {ex.Message}", RecordKind.Error);
                throw ex;
            }

            ConnectionsListener = new TcpListener(IPAddress.Any, DefaultServerValues.FtpControlPort);
            ConnectionsListener.Start();

            logger.Log($"Started the server at {DefaultServerValues.ServerExternalIP}:{DefaultServerValues.FtpControlPort}", RecordKind.Status);

            while (true)
                try
                {
                    var connectedClient = ConnectionsListener.AcceptTcpClient();

                    ActionsTracker.UserConnected(null, connectedClient.Client.RemoteEndPoint);

                    var controlConnection = DiContainer.Provider.Resolve<ControlConnection>();
                    controlConnection.Initialize(connectedClient, IsEncryptionEnabled);
                    
                    var cts = new CancellationTokenSource();
                    cts.Token.Register(() => controlConnection.Dispose());
                    connections.Add(Task.Run(() => controlConnection.InitiateConnection()), cts);
                }
                catch (DirectoryNotFoundException)
                {
                    logger.Log("Wrong server base directory was provided.", RecordKind.Error);
                    Dispose();
                    return;
                }// when server is disposed, these exceptions are thrown.
                catch (InvalidOperationException) { return; }
                catch (SocketException){ return; }
        }

        /// <summary>
        /// Cancels and removes all tasks in <see cref="connections"/> if flag is true,
        /// Waits for tasks to complete / removes all tasks if flag is false.
        /// </summary>
        /// <param name="waitForUsersToDisconnect"></param>
        /// <returns></returns>
        public async Task Stop(bool waitForUsersToDisconnect)
        {
            try
            {
                if (!waitForUsersToDisconnect)
                {
                    foreach (var task in connections)
                    {
                        if (task.Key.IsCanceled || task.Key.IsCompleted)
                            continue;

                        connections[task.Key].Cancel();
                        connections[task.Key].Dispose();
                    }

                    return;
                }

                logger.Log("Waiting for all users to disconnect.", RecordKind.Status);

                if (connections.Count().Equals(0))
                    return;

                await Task.WhenAll(connections.Keys.ToArray());
            }
            finally
            {
                Dispose();
                logger.Log("Stopped the server.", RecordKind.Status);
            }

        }
    }
}
