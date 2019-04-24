using System;
using System.Threading.Tasks;
using DenCloud.Core.Authentication;
using DenCloud.Core.Data;
using DenCloud.Core.FileSystem;

namespace DenCloud.Core.Helpers
{
    public class DatabaseHelper
    {
        public DatabaseHelper(IFtpFileSystemProvider<FileSystemEntry> FileSystemProvider)
        {
            this.FileSystemProvider = FileSystemProvider;
        }

        private IFtpFileSystemProvider<FileSystemEntry> FileSystemProvider { get; set; }

        public async Task NewRecord(string username, string password)
        {
            if (username.ToUpperInvariant() == "ANONYMOUS")
                throw new InvalidOperationException("Could not add anonymous to database.");

            var userId = Hasher.GetHash(username);

            using (var db = new ApplicationDbContext())
            {
                var user = db.Users.Find(userId);

                if (user != null) throw new InvalidOperationException("User with such name already exists");

                db.Users.Add(new FtpUser
                {
                    Id = userId,
                    Name = username,
                    PasswordHash = Hasher.GetHash(password),
                    StorageBytesTotal = DefaultServerValues.DefaultCloudStorageVolume
                });

                await db.SaveChangesAsync();
            }
        }

        public StorageInfo GetStorageInformation(string username)
        {
            //It's a public account
            if (username.ToUpperInvariant() == "ANONYMOUS")
            {
                FileSystemProvider.Initialize(username);
                long storageOccupied = FileSystemProvider.GetOccupiedDirectoryorFileSpace(FileSystemProvider.GetUserBaseFtpDirectory(username));
                return new StorageInfo()
                {
                    BytesFree = DefaultServerValues.DefaultCloudStorageVolume - storageOccupied,
                    BytesOccupied = storageOccupied,
                    BytesTotal = DefaultServerValues.DefaultCloudStorageVolume
                };
            }

            string userId = Hasher.GetHash(username); 

            using (var db = new ApplicationDbContext())
            {
                var dbUser = db.Users.Find(userId);
                long storageTotal = dbUser.StorageBytesTotal;
                FileSystemProvider.Initialize(username);
                long storageOccupied = FileSystemProvider.GetOccupiedDirectoryorFileSpace(FileSystemProvider.GetUserBaseFtpDirectory(username));
                return new StorageInfo()
                {
                    BytesFree = storageTotal - storageOccupied,
                    BytesOccupied = storageOccupied,
                    BytesTotal = storageTotal
                };
            }
        }


    }
}