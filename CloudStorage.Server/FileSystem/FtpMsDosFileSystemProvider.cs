using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudStorage.Server.FileSystem {
    public class FtpMsDosFileSystemProvider : FtpUnixFileSystemProvider {
        public FtpMsDosFileSystemProvider()
        {
            currentSeparator = Path.DirectorySeparatorChar;
            alternateSeparator = Path.AltDirectorySeparatorChar;
        }
    }
}
