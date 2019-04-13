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

        /// <summary>
        /// You MUST instantiate this property using <see cref="DiConfigBuilder"/>.
        /// </summary>
        public static DependencyProvider Provider { get; set; }
        
        /// <summary>
        /// Value indicating if container was successfully constructed
        /// </summary>
        public static bool IsConstructed { get; set; } = false;

        /// <summary>
        /// This method is being called before ftp server starts, to validate DiContainer.
        /// </summary>
        public static void ValidateProvider()
        {
            if (!IsConstructed)
                throw new ApplicationException("DiContainer was not constructed. Use Construct() method to construct it.");

            Provider.ValidateConfig();
        }

        /// <summary>
        /// Must call this method for DiContainer to work.
        /// </summary>
        /// <param name="configBuilder"></param>
        public static void Construct(DiConfigBuilder configBuilder)
        {
            if (!configBuilder.Constructed)
            {
                throw new InvalidOperationException("Provide a constructed DiConfigBuilder.");
            }

            Provider = new DependencyProvider(configBuilder.config);

            IsConstructed = true;
        }
    }
}
