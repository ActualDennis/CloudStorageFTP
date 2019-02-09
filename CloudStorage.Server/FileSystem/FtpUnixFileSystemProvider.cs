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

        private static char msDosSeparator => Path.DirectorySeparatorChar;
        private static char unixSeparator => Path.AltDirectorySeparatorChar;

        public FtpUnixFileSystemProvider(string serverBaseDirectory)
        {
            BaseDirectory = serverBaseDirectory;
        }

        /// <summary>
        /// Server's base directory(local), where folders of all users are stored.
        /// </summary>
        private string BaseDirectory
        {
            get => baseDirectory;
            set
            {
                if (Directory.Exists(value)) baseDirectory = value;
                else throw new DirectoryNotFoundException($"{value} was not found.");
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
            UserBaseDirectory = BaseDirectory + unixSeparator + username;

            UserName = username;

            if (!Directory.Exists(UserBaseDirectory)) Directory.CreateDirectory(UserBaseDirectory);

            workingDirectory = unixSeparator.ToString();
            //unix will be used as standard to use on this machine
            UserBaseDirectory.Replace(msDosSeparator, unixSeparator);
        }

        /// <summary>
        /// Used by other classes to get for example storage space user occupies
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public string GetUserBaseFtpDirectory(string userName)
        {
            return unixSeparator.ToString();
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
            if (path == unixSeparator.ToString())
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

            if (from == UserBaseDirectory.Trim(unixSeparator) || from == BaseDirectory)
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
            if (workingDirectory == unixSeparator.ToString())
                return;

            workingDirectory = workingDirectory.Substring(0, workingDirectory.LastIndexOf(unixSeparator) + 1);
            //Delete / at the end if this is not root
            if (workingDirectory != unixSeparator.ToString())
                workingDirectory = workingDirectory.TrimEnd(unixSeparator);
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
            if (path == unixSeparator.ToString())
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
            if (chosenPath.Contains(msDosSeparator.ToString()))
                throw new MsDosPathNotSupportedException("Ms dos - like path is not supported. Consider using unix-like path.");

            if (chosenPath == unixSeparator.ToString()) return unixSeparator.ToString();

            var localPath = GetLocalPath(chosenPath).TrimEnd(unixSeparator);

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
            if (ftpPath.Contains(msDosSeparator.ToString()))
                throw new MsDosPathNotSupportedException("Ms dos - like path is not supported. Consider using unix-like path.");

            //2 possible cases here: either server will send full path to go to
            //i.e /anonymous/SomeRandomFolder
            //or relative path which in upper case while we are in /anonymous directory should be : SomeRandomFolder

            return ftpPath.StartsWith(unixSeparator.ToString()) // First case: absolute path: it must start with '/'
                ? FtpToLocalPath(ftpPath)
                : FtpToLocalPath(
                    workingDirectory == unixSeparator.ToString() //Second case: relative path : append '/' to the end of previous ftp path if it's not root
                        ? workingDirectory + ftpPath
                        : workingDirectory + unixSeparator.ToString() + ftpPath);
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