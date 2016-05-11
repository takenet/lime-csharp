using Lime.Client.TestConsole.ViewModels;
using Lime.Protocol;

namespace Lime.Client.TestConsole.Macros
{
    [Macro(Name = "Send set presence available", Category = "Command", IsActiveByDefault = false)]
    public class SendSetPresenceAvailableMacro : SendTemplateMacroBase
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

        protected override string TemplateName => "Set presence available";
    }
}