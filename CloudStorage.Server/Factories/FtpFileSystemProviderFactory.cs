using CloudStorage.Server.Data;
using CloudStorage.Server.FileSystem;

namespace CloudStorage.Server.Factories
{
    public class FtpFileSystemProviderFactory : IFtpFileSystemProviderFactory<FileSystemEntry>
    {
        public IFtpFileSystemProvider<FileSystemEntry> NewFileSysProvider(FtpFileSystemProvider providerType, string serverBaseDirectory)
        {
            switch (providerType)
            {
                case FtpFileSystemProvider.FtpUNIX:
                    return new FtpUnixFileSystemProvider(serverBaseDirectory);
                default:
                    return new FtpUnixFileSystemProvider(serverBaseDirectory);
            }
        }
    }
}
