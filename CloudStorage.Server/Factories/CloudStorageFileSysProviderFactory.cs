using CloudStorage.Server.Data;
using CloudStorage.Server.FileSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudStorage.Server.Factories
{
    public class CloudStorageFileSysProviderFactory
    {
        public ICloudStorageFileSystemProvider NewFileSysProvider(FtpFileSystemProvider providerType, string serverBaseDirectory)
        {
            switch (providerType)
            {
                case FtpFileSystemProvider.FtpUNIX:
                    return new CloudStorageUnixFileSystemProvider(serverBaseDirectory);
                default:
                    return new CloudStorageUnixFileSystemProvider(serverBaseDirectory);
            }
        }
    }
}
