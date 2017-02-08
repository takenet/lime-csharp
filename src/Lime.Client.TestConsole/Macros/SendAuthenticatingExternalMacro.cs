using Lime.Protocol.Security;

namespace Lime.Client.TestConsole.Macros
{

    [Macro(Name = "Send authenticating external", Category = "Session", IsActiveByDefault = false, Order = 1)]
    public class SendAuthenticatingExternalMacro : SendAuthenticatingMacroBase
    {
        protected override string TemplateName => "Authenticating external";

        protected override AuthenticationScheme Scheme => AuthenticationScheme.External;
    }
}
