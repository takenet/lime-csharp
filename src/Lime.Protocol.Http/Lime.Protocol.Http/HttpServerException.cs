using System;
using System.Collections.Generic;
using System.Text;

namespace Lime.Protocol.Http
{
    public class HttpServerException : Exception
    {
        public HttpServerException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }
}
