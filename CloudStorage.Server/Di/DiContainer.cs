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
        public static void ValidateConfig()
        {
            try
            {
                Provider.ValidateConfig();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Dependency injection error: {ex.Message}.");
            }
        }
    }
}
