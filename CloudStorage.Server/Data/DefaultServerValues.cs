namespace CloudStorage.Server.Data
{
    public class DefaultServerValues
    {
        public static int FtpControlPort;

        public static string BaseDirectory;

        public static string CertificateLocation;

        public static string ServerExternalIP;

        public const int MinRecommendedBufferSize = 4096;

        public const int MaxRecommendedBufferSize = 81920;
        /// <summary>
        /// Default storage volume for one user is 2 GB
        /// </summary>
        public const long DefaultCloudStorageVolume = 1024 * 1024 * 1024 * 2L;

        public static string LoggingPath;
    }
}