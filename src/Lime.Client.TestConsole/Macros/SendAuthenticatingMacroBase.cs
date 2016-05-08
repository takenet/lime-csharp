using System.Linq;
using Lime.Client.TestConsole.ViewModels;
using Lime.Protocol;
using Lime.Protocol.Security;

namespace Lime.Client.TestConsole.Macros
{
    public abstract class SendAuthenticatingMacroBase : SendTemplateMacroBase
    {
        protected override bool ShouldSendTemplate(EnvelopeViewModel envelopeViewModel)
        {
            var session = envelopeViewModel.Envelope as Session;
            if (session != null &&
                session.State == SessionState.Authenticating &&
                session.SchemeOptions != null &&
                session.SchemeOptions.Contains(Scheme))
            {
                return true;
            }

            return false;
        }


        protected abstract AuthenticationScheme Scheme { get; }

    }
}