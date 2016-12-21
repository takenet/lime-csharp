using System;

namespace Lime.Protocol.Listeners
{
    [AttributeUsage(AttributeTargets.Method)]
    public class CommandProcessorAttribute : Attribute
    {
        public string ResourceType { get; set; }

        public string UriTemplate { get; set; }

        public CommandMethod Method { get; set; }        
    }
}