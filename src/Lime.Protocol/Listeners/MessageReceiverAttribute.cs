using System;

namespace Lime.Protocol.Listeners
{
    [AttributeUsage(AttributeTargets.Method)]
    public class MessageReceiverAttribute : Attribute
    {
        public string ContentType { get; set; }
    }
}