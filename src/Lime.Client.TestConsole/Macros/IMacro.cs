using Lime.Client.TestConsole.ViewModels;
using Lime.Protocol.Network;
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
}
