using System.Linq;
using Lime.Client.TestConsole.ViewModels;
using Lime.Protocol;

namespace Lime.Client.TestConsole.Macros
{
    [Macro(Name = "Send negotiating none/none", Category = "Session", IsActiveByDefault = false, Order = 1)]
    public class SendNegotiatingNoneNoneMacro : SendTemplateMacroBase
    {
        protected override bool ShouldSendTemplate(EnvelopeViewModel envelopeViewModel)
        {
            var session = envelopeViewModel.Envelope as Session;
            if (session != null &&
                session.State == SessionState.Negotiating &&
                session.CompressionOptions != null &&
                session.CompressionOptions.Contains(SessionCompression.None) &&
                session.EncryptionOptions != null &&
                session.EncryptionOptions.Contains(SessionEncryption.None))
            {
                return true;
            }

            return false;
        }

        protected override string TemplateName => "Negotiating none/none";
    }
}
