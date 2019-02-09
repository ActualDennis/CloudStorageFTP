using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudStorage.Server.FileSystem
{
    public class MsDosPathNotSupportedException : Exception
    {
        public MsDosPathNotSupportedException(string message) : base(message)
        { }

        public MsDosPathNotSupportedException(string message, Exception inner) :
            base(message, inner)
        { }
    }
}
