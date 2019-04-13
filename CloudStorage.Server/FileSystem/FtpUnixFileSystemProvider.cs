using CloudStorage.Server.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CloudStorage.Server.FileSystem
{
    public class FtpUnixFileSystemProvider : DefaultFileSystemProvider,  IFtpFileSystemProvider<FileSystemEntry>
    {
        private string baseDirectory;

        private string workingDirectory;

        protected static char alternateSeparator { get; set; } = Path.DirectorySeparatorChar;
        protected static char currentSeparator { get; set; } = Path.AltDirectorySeparatorChar;

        /// <summary>
        /// Throws exception if <see cref="DefaultServerValues.BaseDirectory"/> is not a path.
        /// </summary>
        public FtpUnixFileSystemProvider()
        {
            BaseDirectory = DefaultServerValues.BaseDirectory;
        }

        /// <summary>
        /// Server's base directory(local), where folders of all users are stored.
        /// </summary>
        private string BaseDirectory
        {
            get => baseDirectory;
            set
            {
                if (!Directory.Exists(value))
                {
                    Directory.CreateDirectory(value);
                }

                baseDirectory = value;
            }
        }
        /// <summary>
        /// Authenticated user's base directory(local). Usually BaseDirectory/UserName
        /// </summary>
        private string UserBaseDirectory { get; set; }

        /// <summary>
        ///     This string stores current ftp path of <see cref="UserName" />
        /// </summary>
        public string WorkingDirectory
        {
            get => workingDirectory;
            set
            {
                if (value == "..")
                {
                    MoveUp();
                    return;
                }

                workingDirectory = GetWorkingDirectory(value);
            }
        }

        public string UserName { get; private set; }

        /// <summary>
        ///     Creates user directory if it's first time user's logged in
        /// </summary>
        public void Initialize(string username)
        {
            UserBaseDirectory = BaseDirectory + currentSeparator + username;

            UserName = username;

            if (!Directory.Exists(UserBaseDirectory)) Directory.CreateDirectory(UserBaseDirectory);

            workingDirectory = currentSeparator.ToString();
            //unix will be used as standard to use on this machine
            UserBaseDirectory.Replace(alternateSeparator, currentSeparator);
        }

        /// <summary>
        /// Used by other classes to get for example storage space user occupies
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public string GetUserBaseFtpDirectory(string userName)
        {
            return currentSeparator.ToString();
        }
        /// <summary>
        /// Enumerates files and directories in path directory. 
        /// If path is null, enumerate <see cref="WorkingDirectory"/>
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public override IEnumerable<FileSystemEntry> EnumerateDirectory(string path)
        {
            var localDir = GetLocalPath(string.IsNullOrEmpty(path) ? workingDirectory : path);
            return base.EnumerateDirectory(localDir);
        }

        public override FileSystemEntry GetFileorDirectoryInfo(string path)
        {
            var localDir = GetLocalPath(string.IsNullOrEmpty(path) ? workingDirectory : path);

            return base.GetFileorDirectoryInfo(localDir);
        }
        
        /// <summary>
        ///     Deletes either a directory or a file, if either of them exist
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public override void Delete(string path)
        {
            if (path == currentSeparator.ToString())
                throw new InvalidOperationException("Could not remove user base directory.");

            path = GetLocalPath(path);

            //Check to determine if this is directory or a file
            base.Delete(path);
        }

        public override void Rename(string from, string to)
        {
            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
                return;

            from = GetLocalPath(from);

            to = GetLocalPath(to);

            if (from == UserBaseDirectory.Trim(currentSeparator) || from == BaseDirectory)
                throw new InvalidOperationException("Could not rename root folder.");

            base.Rename(from, to);
        }


        // example of usage : MKD New folder --> create currentDir/New folder
        /// <summary>
        ///     Creates directory under working directory. TODO: Throws a LOT of exceptions
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public override void CreateDirectory(string path)
        {
            //only allow creating folders in user's own directory
            path = GetLocalPath(path);

            if (!path.IndexOf(UserBaseDirectory).Equals(-1))
                base.CreateDirectory(path);
            else
                throw new FormatException("You are allowed to create folders only in your own directory.");
        }


        public void MoveUp()
        {
            if (workingDirectory == currentSeparator.ToString())
                return;

            workingDirectory = workingDirectory.Substring(0, workingDirectory.LastIndexOf(currentSeparator) + 1);
            //Delete / at the end if this is not root
            if (workingDirectory != currentSeparator.ToString())
                workingDirectory = workingDirectory.TrimEnd(currentSeparator);
        }

        public override Stream GetFileStream(string ftpPath)
        {
            var localDir = GetLocalPath(ftpPath);
            return base.GetFileStream(localDir);
        }

        public new long GetOccupiedDirectoryorFileSpace(string path)
        {
            var localDir = GetLocalPath(path);
            return base.GetOccupiedDirectoryorFileSpace(localDir);
        }

        /// <summary>
        ///     Adds BaseDirectory to the start of ftp path string
        /// </summary>
        /// <param name="path">ftp path</param>
        /// <returns></returns>
        private string FtpToLocalPath(string path)
        {
            if (path == currentSeparator.ToString())
                return UserBaseDirectory;

            return UserBaseDirectory + path;
        }

        /// <summary>
        ///     Gets a new ftp path value, checks if it's correct , returns corrected version
        ///     Or throws exception otherwise.
        /// </summary>
        /// <param name="chosenPath"></param>
        /// <returns></returns>
        public string GetWorkingDirectory(string chosenPath)
        {
            if (chosenPath.Contains(alternateSeparator.ToString()))
                throw new WrongPathFormatException($"Wrong path format was chosen. Current file system type is {(currentSeparator == '/' ? "unix-like" : "ms-dos like")}.");

            if (chosenPath == currentSeparator.ToString()) return currentSeparator.ToString();

            var localPath = GetLocalPath(chosenPath).TrimEnd(currentSeparator);

            if (!localPath.IndexOf(UserBaseDirectory).Equals(-1))
            {
                if (Directory.Exists(localPath))
                    return localPath.Replace(UserBaseDirectory, string.Empty);
                else
                {
                    Directory.CreateDirectory(localPath);
                    return localPath.Replace(UserBaseDirectory, string.Empty);
                }
                
            }

            throw new FormatException(
                "Path was not found.");
        }

        private string GetLocalPath(string ftpPath)
        {
            if (ftpPath.Contains(alternateSeparator.ToString()))
                throw new WrongPathFormatException($"Wrong path format was chosen. Current file system type is {(currentSeparator == '/' ? "unix-like" : "ms-dos like")}.");

            //2 possible cases here: either server will send full path to go to
            //i.e /anonymous/SomeRandomFolder
            //or relative path which in upper case while we are in /anonymous directory should be : SomeRandomFolder

            return ftpPath.StartsWith(currentSeparator.ToString()) // First case: absolute path: it must start with '/'
                ? FtpToLocalPath(ftpPath)
                : FtpToLocalPath(
                    workingDirectory == currentSeparator.ToString() //Second case: relative path : append '/' to the end of previous ftp path if it's not root
                        ? workingDirectory + ftpPath
                        : workingDirectory + currentSeparator.ToString() + ftpPath);
        }

        public override string GetFileLastModifiedTime(string path)
        {
            var localPath = GetLocalPath(path);
            return base.GetFileLastModifiedTime(localPath);
        }

        public override FileStream CreateNewFileorOverwrite(string path)
        {
            var localPath = GetLocalPath(path);
            return base.CreateNewFileorOverwrite(localPath);
        }

        public override FileStream CreateNewFile(string path)
        {
            var localPath = GetLocalPath(path);
            return base.CreateNewFile(localPath);
        }

    }
}