using CloudStorage.Server.Exceptions;
using CloudStorage.Server.Helpers;
using System.IO;

namespace CloudStorage.Server.FileSystem
{
    public class CloudStorageUnixFileSystemProvider : FtpUnixFileSystemProvider, ICloudStorageFileSystemProvider
    {
        public CloudStorageUnixFileSystemProvider(string serverBaseDirectory) : base(serverBaseDirectory)
        {
        }

        public override FileStream CreateNewFile(string path)
        {
            var storageInfo = DatabaseHelper.GetStorageInformation(UserName);

            if(storageInfo.BytesOccupied >= storageInfo.BytesTotal)
                throw new UserOutOfSpaceException("Can't copy the files because your cloud storage limit exceeded.");

            return base.CreateNewFile(path);
        }

        public override FileStream CreateNewFileorOverwrite(string path)
        {
            var storageInfo = DatabaseHelper.GetStorageInformation(UserName);

            if (storageInfo.BytesOccupied >= storageInfo.BytesTotal)
                throw new UserOutOfSpaceException("Can't copy the files because your cloud storage limit exceeded.");

            return base.CreateNewFileorOverwrite(path);
        }

    }
}
