using DenCloud.Core.Di;
using DenCloud.Core.Exceptions;
using DenCloud.Core.Helpers;
using System;
using System.IO;

namespace DenCloud.Core.FileSystem
{
    public class CloudStorageUnixFileSystemProvider : FtpUnixFileSystemProvider, IFtpFileSystemProvider<FileSystemEntry>
    {
        public CloudStorageUnixFileSystemProvider(Lazy<DatabaseHelper> DbHelper)
        {
            this.DbHelper = DbHelper;
        }

        private Lazy<DatabaseHelper> DbHelper { get; set; }

        public override FileStream CreateNewFile(string path)
        {
            var storageInfo = DbHelper.Value.GetStorageInformation(UserName);

            if(storageInfo.BytesOccupied >= storageInfo.BytesTotal)
                throw new UserOutOfSpaceException("Can't copy the files because your cloud storage limit exceeded.");

            return base.CreateNewFile(path);
        }

        public override FileStream CreateNewFileorOverwrite(string path)
        {
            var storageInfo = DbHelper.Value.GetStorageInformation(UserName);

            if (storageInfo.BytesOccupied >= storageInfo.BytesTotal)
                throw new UserOutOfSpaceException("Can't copy the files because your cloud storage limit exceeded.");

            return base.CreateNewFileorOverwrite(path);
        }

    }
}
