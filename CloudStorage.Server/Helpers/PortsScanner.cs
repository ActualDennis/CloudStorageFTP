using CloudStorage.Server.Connections;
using CloudStorage.Server.Data;
using CloudStorage.Server.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CloudStorage.Server.Helpers {
    public static class PortsScanner {
        public static void ScanForOpenPorts()
        {
            if (ActionsTracker.UsersInfo.Count() != 0)
            {
                Console.WriteLine($"There are active users on this server. Try again when there are no any.");
                return;
            }

            for (int i = DataConnection.MinPort; i < DataConnection.MaxPort; ++i)
            {
                var listener = new TcpListener(IPAddress.Any, i);
                listener.Start();
            }
            for (int i = DataConnection.MinPort; i < DataConnection.MaxPort; ++i)
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
    }
}
