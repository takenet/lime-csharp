using System;

namespace Lime.Transport.Tcp
{
    public class BufferOverflowException : Exception
    {
        public BufferOverflowException(string message)
            : base(message)
        {
            

        }
    }
}