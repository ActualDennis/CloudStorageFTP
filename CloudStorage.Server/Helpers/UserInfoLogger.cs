using CloudStorage.Server.Data;
using CloudStorage.Server.Di;
using CloudStorage.Server.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CloudStorage.Server.Helpers {
    public static class UserInfoLogger {
        public static void PrintUsersInfo()
        {
            var enumerable = ActionsTracker.UsersInfo.AsEnumerable();
            if ((enumerable == null) || (enumerable.Count() == 0))
            {
                Console.WriteLine("No users are currently on the server.");
                return;
            }

            foreach (var user in enumerable)
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

                    var storageInfo = DiContainer.Provider.Resolve<DatabaseHelper>().GetStorageInformation(user.Value.UserName);

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
