using Lime.Protocol.Security;

namespace Lime.Client.TestConsole.Macros
{
    [Macro(Name = "Send authenticating plain", Category = "Session", IsActiveByDefault = false, Order = 1)]
    public class SendAuthenticatingPlainMacro : SendAuthenticatingMacroBase
    {
        protected override string TemplateName => "Authenticating plain";

        protected override AuthenticationScheme Scheme => AuthenticationScheme.Plain;
    }
}
