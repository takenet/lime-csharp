using Lime.Protocol.Security;

namespace Lime.Client.TestConsole.Macros
{
    [Macro(Name = "Send authenticating transport", Category = "Session", IsActiveByDefault = false, Order = 1)]
    public class SendAuthenticatingTransportMacro : SendAuthenticatingMacroBase
    {
        protected override string TemplateName => "Authenticating transport";

        protected override AuthenticationScheme Scheme => AuthenticationScheme.Transport;
    }
}