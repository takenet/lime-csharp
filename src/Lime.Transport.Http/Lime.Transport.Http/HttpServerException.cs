using System;

namespace Lime.Transport.Http.Protocol
{
    public class HttpServerException : Exception
    {
        public HttpServerException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }
}
