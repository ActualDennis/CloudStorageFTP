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

        /// <summary>
        /// Config to use while DiContainer construction
        /// </summary>
        public DiConfiguration config { get; set; }

        public DiConfigFlags ConfigFlags { get; set; }

        /// <summary>
        /// Value indicates if this builder is ready to be passed to DiContainer.
        /// </summary>
        public bool Constructed
        {
            get =>
                ConfigFlags.HasFlag(DiConfigFlags.NecessaryClassesUsed)
                && ConfigFlags.HasFlag(DiConfigFlags.LoggerUsed)
                && ConfigFlags.HasFlag(DiConfigFlags.FilesystemUsed)
                && ConfigFlags.HasFlag(DiConfigFlags.AuthUsed);
        }

        /// <summary>
        /// You must call this method to obtain minimum ftp server functionality.
        /// </summary>
        public void UseNeccessaryClasses()
        {
            config.RegisterTransient<FtpServer, FtpServer>();
            config.RegisterTransient<DataConnection, DataConnection>();
            config.RegisterTransient<FtpCommandFactory, FtpCommandFactory>();
            config.RegisterSingleton<DatabaseHelper, DatabaseHelper>();
            config.RegisterTransient<ControlConnection, ControlConnection>();
            ConfigFlags |= DiConfigFlags.NecessaryClassesUsed;
        }

        /// <summary>
        /// You must call this method to obtain logger.
        /// </summary>
        /// <param name="loggerType">Type of logger to use</param>
        /// <param name="UseDefault">Use default logger(file logger)</param>
        public void UseLogger(Type loggerType, bool UseDefault)
        {
            if (UseDefault)
            {
                loggerType = typeof(AutomaticFileLogger);
            }
            else
            {
                if (!typeof(ILogger).IsAssignableFrom(loggerType))
                {
                    throw new InvalidOperationException($"{loggerType.ToString()} is not a valid logger.");
                }
            }
        
            config.RegisterSingleton(typeof(ILogger), loggerType);
            ConfigFlags |= DiConfigFlags.LoggerUsed;
        }
        /// <summary>
        ///  You must call this method to obtain authentication.
        /// </summary>
        /// <param name="authType">Type of authentication to use</param>
        /// <param name="UseDefault">Use default authentication(database authentication)</param>
        public void UseAuthentication(Type authType, bool UseDefault)
        {
            if (UseDefault)
            {
                authType = typeof(FtpDbAuthenticationProvider);
            }
            else
            {
                if (!typeof(IAuthenticationProvider).IsAssignableFrom(authType))
                {
                    throw new InvalidOperationException($"{authType.ToString()} is not a valid authentication provider.");
                }
            }

            config.RegisterSingleton(typeof(IAuthenticationProvider), authType);
            ConfigFlags |= DiConfigFlags.AuthUsed;
        }

        /// <summary>
        /// You must call this method to obtain filesystem functionality.
        /// </summary>
        /// <param name="filesystemType">Type of filesystem to use</param>
        /// <param name="UseDefault">Use default filesystem(unix-like)</param>
        public void UseFileSystem(Type filesystemType, bool UseDefault)
        {
            if (UseDefault)
            {
                filesystemType = typeof(CloudStorageUnixFileSystemProvider);
            }
            else
            {
                if (!typeof(ICloudStorageFileSystemProvider).IsAssignableFrom(filesystemType))
                {
                    throw new InvalidOperationException($"{filesystemType.ToString()} is not a valid filesystem.");
                }
            }


            config.RegisterTransient(typeof(ICloudStorageFileSystemProvider), filesystemType);
            ConfigFlags |= DiConfigFlags.FilesystemUsed;
        }
    }
}
