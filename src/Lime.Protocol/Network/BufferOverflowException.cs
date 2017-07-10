using System;

namespace Lime.Protocol.Network
{
    public class BufferOverflowException : Exception
    {
        public BufferOverflowException() : base()
        {
        }

        public BufferOverflowException(string message)
            : base(message)
        {

        }

        public BufferOverflowException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}