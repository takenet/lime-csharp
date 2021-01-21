using System;

namespace Lime.Protocol.Network
{
    public class EnvelopeTooLargeException : Exception
    {
        public EnvelopeTooLargeException(string message) 
            : base(message)
        {
        }

        public EnvelopeTooLargeException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }
}
