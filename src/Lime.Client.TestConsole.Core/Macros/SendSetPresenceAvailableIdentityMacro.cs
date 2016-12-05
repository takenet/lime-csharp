using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lime.Client.TestConsole.ViewModels;
using Lime.Protocol;

namespace Lime.Client.TestConsole.Macros
{
    [Macro(Name = "Send set presence available identity", Category = "Command", IsActiveByDefault = false)]
    public class SendSetPresenceAvailableIdentityMacro : SendTemplateMacroBase
    {
        protected override bool ShouldSendTemplate(EnvelopeViewModel envelopeViewModel)
        {
            var session = envelopeViewModel.Envelope as Session;
            if (session != null &&
                session.State == SessionState.Established)
            {
                return true;
            }

            return false;
        }

        protected override string TemplateName => "Set presence available identity";
    }
}
