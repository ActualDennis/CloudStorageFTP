using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CloudStorage.Server.FileSystem
{
    public interface IFileSystemProvider<T> where T : class
    {
        /// <summary>
        /// Returns T as an information about file or directory 
        /// specified in path variable
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        T GetFileorDirectoryInfo(string path);

        void Rename(string from, string to);

        void Delete(string path);

        void CreateDirectory(string path);

        long GetOccupiedDirectoryorFileSpace(string path);

        /// <summary>
        /// Opens file for read and returns it as Stream
        /// </summary>
        /// <param name="pathToFile"></param>
        /// <returns></returns>
        Stream GetFileStream(string pathToFile);

        /// <summary>
        /// Enumerates directory specified in path variable.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        IEnumerable<T> EnumerateDirectory(string path);

        string GetFileLastModifiedTime(string path);

        FileStream CreateNewFile(string path);

        FileStream CreateNewFileorOverwrite(string path);
    }
}