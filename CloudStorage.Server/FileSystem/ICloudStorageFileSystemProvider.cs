using System.IO;

namespace CloudStorage.Server.FileSystem
{
    public interface ICloudStorageFileSystemProvider : IFtpFileSystemProvider<FileSystemEntry>
    {
        /// <summary>
        /// Checks if user can create new files in his directory
        /// And returns filestream if user didn't exceed his cloud storage volume.
        /// Creates new file if copy is detected.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        new FileStream CreateNewFile(string path);
        /// <summary>
        /// Checks if user can create new files in his directory
        /// And returns filestream if user didn't exceed his cloud storage volume.
        /// Overwrites file if copy is detected.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        new FileStream CreateNewFileorOverwrite(string path);
    }
}
