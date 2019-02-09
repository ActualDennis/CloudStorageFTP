using CloudStorage.Server.Data;
using CloudStorage.Server.FileSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudStorage.Server.Factories
{
    public interface IFtpFileSystemProviderFactory<T> where T : class
    {
        IFtpFileSystemProvider<T> NewFileSysProvider(FtpFileSystemProvider providerType, string serverBaseDirectory); 
    }
}
