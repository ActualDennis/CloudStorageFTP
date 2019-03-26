using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudStorage.Server.FileSystem
{
    public class WrongPathFormatException : Exception
    {
        public WrongPathFormatException(string message) : base(message)
        { }

        public WrongPathFormatException(string message, Exception inner) :
            base(message, inner)
        { }
    }
}
