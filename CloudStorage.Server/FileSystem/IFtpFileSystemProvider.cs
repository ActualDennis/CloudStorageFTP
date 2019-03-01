using System.Collections.Generic;

namespace CloudStorage.Server.FileSystem
{
    public interface IFtpFileSystemProvider<T> : IFileSystemProvider<T> where T : class
    {
        /// <summary>
        /// Ftp Working directory of user, as seen on client-side
        /// </summary>
        string WorkingDirectory { get; set; }

        // <summary>
        /// Gets every user's base directory.
        /// Usually it's \ or /
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        string GetUserBaseFtpDirectory(string userName);

        /// <summary>
        /// This procedure should be called after user authenticated, 
        /// to initialize his base directory
        /// </summary>
        void Initialize(string UserName);

        /// <summary>
        /// Enumerates directory specified in path variable.
        /// If it's null, user's base directory is assumed.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        new IEnumerable<T> EnumerateDirectory(string path);

        /// <summary>
        /// Goes to parent directory.
        /// if this is not possible, does nothing
        /// </summary>
        void MoveUp();

        new string GetFileLastModifiedTime(string path);
    }
}