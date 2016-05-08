using System;

namespace Lime.Client.TestConsole.Macros
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MacroAttribute : Attribute
    {
        public string Name { get; set; }

        public string Category { get; set; }

        public bool IsActiveByDefault { get; set; }

        public int Order { get; set; }
    }
}