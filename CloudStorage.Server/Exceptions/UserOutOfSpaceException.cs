using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudStorage.Server.Exceptions
{
    public class UserOutOfSpaceException : Exception
    {
        public UserOutOfSpaceException(string message) : base(message)
        { }
        
        public UserOutOfSpaceException(string message, Exception inner) :
            base(message, inner)
        { }
    }
}
