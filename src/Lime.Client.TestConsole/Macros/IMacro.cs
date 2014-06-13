using Lime.Client.TestConsole.ViewModels;
using Lime.Protocol.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Client.TestConsole.Macros
{
    public interface IMacro
    {
        Task ProcessAsync(EnvelopeViewModel envelopeViewModel, SessionViewModel sessionViewModel);
    }


    [AttributeUsage(AttributeTargets.Class)]
    public class MacroAttribute : Attribute
    {
        public string Name { get; set; }

        public string Category { get; set; }

        public bool IsActiveByDefault { get; set; }
    }
}
