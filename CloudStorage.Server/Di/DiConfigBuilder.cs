using CloudStorage.Server.Authentication;
using CloudStorage.Server.Connections;
using CloudStorage.Server.Factories;
using CloudStorage.Server.FileSystem;
using CloudStorage.Server.Helpers;
using CloudStorage.Server.Logging;
using DenInject.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudStorage.Server.Di
{
    /// <summary>
    /// You must use this class to create a config and then create <see cref="DependencyProvider"/> with it.
    /// </summary>
    public class DiConfigBuilder
    {
        public DiConfigBuilder()
        {
            this.config = new DiConfiguration();
        }

        private DiConfiguration config { get; set; }

        /// <summary>
        /// You must call this method to obtain minimum ftp server functionality.
        /// </summary>
        public void UseNeccessaryClasses()
        {
            config.RegisterSingleton<FtpServer, FtpServer>();
            config.RegisterTransient<DataConnection, DataConnection>();
            config.RegisterTransient<FtpCommandFactory, FtpCommandFactory>();
            config.RegisterSingleton<DatabaseHelper, DatabaseHelper>();
            config.RegisterTransient<ControlConnection, ControlConnection>();
        }

        public void UseLogger(Type loggerType, ObjLifetime lifetime)
        {

            if (!typeof(ILogger).IsAssignableFrom(loggerType))
            {
                throw new InvalidOperationException($"{loggerType.ToString()} is not a valid logger.");
            }

            switch (lifetime)
            {
                case ObjLifetime.Singleton:
                    {
                        config.RegisterSingleton(typeof(ILogger), loggerType);
                        break;
                    }
                case ObjLifetime.Transient:
                    {
                        config.RegisterTransient(typeof(ILogger), loggerType);
                        break;
                    }
            }
        }

        public void UseAuthentication(Type authType, ObjLifetime lifetime)
        {
            if (!typeof(IAuthenticationProvider).IsAssignableFrom(authType))
            {
                throw new InvalidOperationException($"{authType.ToString()} is not a valid authentication provider.");
            }

            switch (lifetime)
            {
                case ObjLifetime.Singleton:
                    {
                        config.RegisterSingleton(typeof(IAuthenticationProvider), authType);
                        break;
                    }
                case ObjLifetime.Transient:
                    {
                        config.RegisterTransient(typeof(IAuthenticationProvider), authType);
                        break;
                    }
            }
        }
        
        public void UseFileSystem(Type filesystemType, ObjLifetime lifetime)
        {
            if (!typeof(ICloudStorageFileSystemProvider).IsAssignableFrom(filesystemType))
            {
                throw new InvalidOperationException($"{filesystemType.ToString()} is not a valid filesystem.");
            }

            switch (lifetime) 
            {
                case ObjLifetime.Singleton:
                    {
                        config.RegisterSingleton(typeof(ICloudStorageFileSystemProvider), filesystemType);
                        break;
                    }
                case ObjLifetime.Transient:
                    {
                        config.RegisterTransient(typeof(ICloudStorageFileSystemProvider), filesystemType);
                        break;
                    }
            }
        }
    }
}
