using CloudStorage.Server.Di;
using CloudStorage.Server.Exceptions;
using CloudStorage.Server.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudStorage.Server.FileSystem {
    public class CloudStorageMsDosFileSystemProvider : FtpMsDosFileSystemProvider, ICloudStorageFileSystemProvider {
        public CloudStorageMsDosFileSystemProvider()
        {
           
        }

        public override FileStream CreateNewFile(string path)
        {
            var storageInfo = DiContainer.Provider.Resolve<DatabaseHelper>().GetStorageInformation(UserName);

            if (storageInfo.BytesOccupied >= storageInfo.BytesTotal)
                throw new UserOutOfSpaceException("Can't copy the files because your cloud storage limit exceeded.");

            return base.CreateNewFile(path);
        }

        public override FileStream CreateNewFileorOverwrite(string path)
        {
            var storageInfo = DiContainer.Provider.Resolve<DatabaseHelper>().GetStorageInformation(UserName);

            if (storageInfo.BytesOccupied >= storageInfo.BytesTotal)
                throw new UserOutOfSpaceException("Can't copy the files because your cloud storage limit exceeded.");

            return base.CreateNewFileorOverwrite(path);
        }
    }
}
