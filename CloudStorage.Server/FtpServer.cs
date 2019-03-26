using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using CloudStorage.Server.Authentication;
using CloudStorage.Server.Data;
using CloudStorage.Server.Di;
using CloudStorage.Server.Factories;
using CloudStorage.Server.FileSystem;
using CloudStorage.Server.Logging;
using CloudStorage.Server.Misc;

namespace CloudStorage.Server {

    public class FtpServer : IDisposable
    {
        public FtpServer(
            ILogger logger)
        {
            ServerDirectory = DefaultServerValues.BaseDirectory;
            this.logger = logger;
        }

        private TcpListener ConnectionsListener { get; set; }

        private string ServerDirectory { get; }

        private Dictionary<Task, CancellationTokenSource> connections { get; set; } = new Dictionary<Task, CancellationTokenSource>();

        private ILogger logger { get; set; }

        public void Dispose()
        {
            ConnectionsListener.Stop();
            connections = null;
        }
        /// <summary>
        /// Call this method if you want to start listening for incoming requests 
        /// and allow users manage their virtual storage
        /// </summary>
        /// <returns></returns>
        public async Task Start(int Port, bool IsEncryptionEnabled)
        {
            ConnectionsListener = new TcpListener(IPAddress.Any, Port);
            ConnectionsListener.Start();
            while (true)
                try
                {
                    var connectedClient = ConnectionsListener.AcceptTcpClient();

                    ActionsTracker.UserConnected(null, connectedClient.Client.RemoteEndPoint);

                    var controlConnection = DiContainer.Provider.Resolve<ControlConnection>();
                    controlConnection.Initialize(connectedClient, IsEncryptionEnabled);

                    var cts = new CancellationTokenSource();
                    connections.Add(Task.Run(() => controlConnection.InitiateConnection(cts.Token)), cts);
                }
                catch (DirectoryNotFoundException)
                {
                    logger.Log("Wrong server base directory was provided.", RecordKind.Error);
                    Dispose();
                    break;
                }
                catch (SocketException)
                {
                    logger.Log("Stopped the server.", RecordKind.Status);
                }
        }
        /// <summary>
        /// Cancels and removes all tasks in <see cref="connections"/> if flag is true,
        /// Waits for tasks to complete and removes all tasks if flag is false.
        /// </summary>
        /// <param name="waitForUsersToDisconnect"></param>
        /// <returns></returns>
        public async Task Stop(bool waitForUsersToDisconnect)
        {
            var tasksToRemove = new List<Task>();

            try
            {
                if (!waitForUsersToDisconnect)
                {
                    foreach (var task in connections.Keys)
                    {
                        if (task.IsCanceled || task.IsCompleted)
                        {
                            tasksToRemove.Add(task);
                            continue;
                        }

                        connections[task].Cancel();
                    }

                    foreach(var task in tasksToRemove)
                    {
                        connections.Remove(task);
                    }

                    return;
                }

                Console.WriteLine("Waiting for all users to disconnect.");

                while (connections.Count != 0)
                {
                    tasksToRemove = new List<Task>();
                    foreach (var task in connections.Keys)
                    {
                        if (task.IsCanceled || task.IsCompleted)
                        {
                            tasksToRemove.Add(task);
                            continue;
                        }
                    }

                    foreach (var task in tasksToRemove)
                    {
                        connections.Remove(task);
                    }

                    await Task.Delay(100);
                }
            }
            finally
            {
                Dispose();
                Console.WriteLine("Stopped the server.");
            }
           
        }
    }
}