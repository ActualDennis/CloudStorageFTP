using CloudStorage.Server.Authentication;
using CloudStorage.Server.Connections;
using CloudStorage.Server.Data;
using CloudStorage.Server.Helpers;
using CloudStorage.Server.Misc;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace CloudStorage.Server
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            Initialize();
            var server = new FtpServer(
                ftpPort: DefaultServerValues.FtpControlPort,
                IsEncryptionAvailable: File.Exists(DefaultServerValues.CertificateLocation));

            _ = Task.Run(() => server.Start());
            Console.WriteLine("Welcome to CloudStorage FTP server v1.0");
            Console.WriteLine($"Server's endPoint is {DefaultServerValues.ServerExternalIP}:{server.ControlPort}");
            Console.WriteLine("Available commands:");
            Console.WriteLine("'test' - test router for open ports. Available only when there are no users connected.");
            Console.WriteLine("'users' - print info about connected users.");
            Console.WriteLine("'stop' - Stop the server and abort all users' connections.");
            while (true)
            {
                var command = Console.ReadLine().ToUpper();
                switch (command)
                {
                    case "USERS":
                        {
                            PrintUsersInfo();
                            break;
                        }
                    case "STOP":
                        {
                            string response;
                            do
                            {
                                Console.WriteLine("Wait for all users to disconnect? [Y] [N]");
                                response = Console.ReadLine().ToUpper();
                            }
                            while ((response[0] != 'Y') && (response[0] != 'N'));

                            await server.Stop(response.ToUpper()[0] == 'Y' ? true : false);
                            Console.ReadLine();
                            Environment.Exit(0);
                            break;
                        }
                    case "TEST":
                        {
                            ScanForOpenPorts();
                            break;
                        }
                }
            }
        }

        private static void Initialize()
        {
            try
            {
                //For server to operate as you want , 
                //you must set desired values in DefaultValues.resx
                //If you are using ssl certificate, it should be without password for now.
                DefaultServerValues.CertificateLocation = DefaultValues.SSLCertificatePath;
                DefaultServerValues.BaseDirectory = DefaultValues.BaseServerDirectory;
                DefaultServerValues.FtpControlPort = int.Parse(DefaultValues.CommandsFtpPort);
                DefaultServerValues.ServerExternalIP = DefaultValues.ServerExternalIP;
                DefaultServerValues.LoggingPath = DefaultValues.LoggingPath;
                DataConnection.MaxPort = int.Parse(DefaultValues.PortRangeMaximum);
                DataConnection.MinPort = int.Parse(DefaultValues.PortRangeMinimum);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Server did not start properly. Fix these errors and try again: {ex.Message}");
                Console.ReadLine();
                Environment.Exit(0);
            }
        }

        private static void ScanForOpenPorts()
        {
            if(ActionsTracker.UsersInfo.Count() != 0)
            {
                Console.WriteLine($"There are active users on this server. Try again when there are no any.");
                return;
            }

            for(int i = DataConnection.MinPort; i < DataConnection.MaxPort; ++i)
            {
                var listener = new TcpListener(IPAddress.Any, i);
                listener.Start();
            }
            for(int i = DataConnection.MinPort; i < DataConnection.MaxPort; ++i)
            {
                try
                {
                    var client = new TcpClient();
                    client.Connect(DefaultServerValues.ServerExternalIP, i);
                    Console.WriteLine($"Port {i} is opened.");
                    client.Close();
                }
                catch
                {
                    Console.WriteLine($"Port {i} is closed.");
                }
            }

        }

        private static void PrintUsersInfo()
        {
            var enumerable = ActionsTracker.UsersInfo.AsEnumerable();
            if((enumerable == null) || (enumerable.Count() == 0))
            {
                Console.WriteLine("No users are currently on the server.");
                return;
            }

            foreach(var user in enumerable)
            {
                Console.WriteLine($"User's endpoint: {((IPEndPoint)user.Key).ToString()}");
                Console.WriteLine(user.Value.IsAuthenticated
                    ? $"Authenticated as : {user.Value.UserName}"
                    : $"Currently not authenticated.");
                string value = user.Value.Security switch
                { 
                    ConnectionSecurity.ControlConnectionSecured => "Securing only command channel.",
                    ConnectionSecurity.DataChannelSecured => "Securing only data channel.",
                    ConnectionSecurity.Both => "Securing both data and command channels.",
                    ConnectionSecurity.NonSecure => "Non-secured.",
                    _ => "Non-secured."
                };

                Console.WriteLine($"User's security: {value}");

                if (user.Value.IsAuthenticated)
                {

                    var storageInfo = DatabaseHelper.GetStorageInformation(user.Value.UserName);
                   
                    Console.WriteLine($"Total storage of user {user.Value.UserName} is {BytesToStringFormatted(storageInfo.BytesTotal)}");
                    Console.WriteLine($"Occupied: {BytesToStringFormatted(storageInfo.BytesOccupied)}");
                    Console.WriteLine($"Free: {BytesToStringFormatted(storageInfo.BytesFree)}");

                }

                Console.WriteLine();
            }
        }

        private static string BytesToStringFormatted(long bytes)
        {
            return bytes switch
            {
                long x when x < 1024 => $"{x} Bytes.",
                long x when (x >= 1024) && (x < 1024 * 1024) => $"{(float)x / 1024} kB.",
                long x when (x >= 1024 * 1024) && (x < 1024 * 1024 * 1024) => $"{(float)x / (1024 * 1024)} MB.",
                long x when (x >= 1024 * 1024 * 1024) => $"{(float)x / (1024 * 1024 * 1024)} GB.",
                _ => "Out of range."
            };
        }
    }
}