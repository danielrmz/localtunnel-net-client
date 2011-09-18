using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LocalTunnel.Library.Exceptions
{
    public class ServiceException : Exception
    {
        public ServiceException(string message)
            : base(message)
        {
        }
    }
}
