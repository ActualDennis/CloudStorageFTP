using CloudStorage.Server.Connections;
using CloudStorage.Server.Data;
using CloudStorage.Server.Di;
using CloudStorage.Server.Helpers;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CloudStorage.Server {
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            Initialize();

            var server = DiContainer.Provider.Resolve<FtpServer>(); 

            _ = Task.Run(() => server.Start(
                FtpFileSystemProvider.FtpUNIX, 
                DefaultServerValues.FtpControlPort, 
                File.Exists(DefaultServerValues.CertificateLocation)));

            Console.WriteLine("Welcome to CloudStorage FTP server v1.0");
            Console.WriteLine($"Server's endPoint is {DefaultServerValues.ServerExternalIP}:{DefaultServerValues.FtpControlPort}");
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
                var settings = XmlConfigParser.ParseSettings();

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

                DiContainer.Initialize();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Server did not start properly. Fix these errors and try again: {ex.Message}");
                Console.ReadLine();
                Environment.Exit(0);
            }
        }

        private static void ScanForOpenPorts()
        {
            PortsScanner.ScanForOpenPorts();
        }

        private static void PrintUsersInfo()
        {
            UserInfoLogger.PrintUsersInfo();
        }
    }
}