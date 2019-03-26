using CloudStorage.Server.Authentication;
using CloudStorage.Server.Connections;
using CloudStorage.Server.Factories;
using CloudStorage.Server.FileSystem;
using CloudStorage.Server.Helpers;
using CloudStorage.Server.Logging;
using DenInject.Core;
using System;

namespace CloudStorage.Server.Di {
    public static class DiContainer {
        public static DependencyProvider Provider { get; set; }

        public static void Initialize()
        {
            var config = new DiConfiguration();
            config.RegisterSingleton<FtpServer, FtpServer>();
            config.RegisterSingleton<ILogger, AutomaticFileLogger>();
            config.RegisterSingleton<IAuthenticationProvider, FtpDbAuthenticationProvider>();
            config.RegisterTransient<DataConnection, DataConnection>();
            config.RegisterTransient<FtpCommandFactory, FtpCommandFactory>();
            config.RegisterSingleton<DatabaseHelper, DatabaseHelper>();
            config.RegisterTransient<ICloudStorageFileSystemProvider, CloudStorageUnixFileSystemProvider>();
            config.RegisterTransient<ControlConnection, ControlConnection>();

            Provider = new DependencyProvider(config);

            try
            {
                Provider.ValidateConfig();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"There was a problem with dependency injection. Program won't start until you fix these errors: {ex.Message}.");
                Console.ReadLine();
                Environment.Exit(0);
            }
        }
    }
}
