using Lime.Protocol.Security;

namespace Lime.Client.TestConsole.Macros
{
    [Macro(Name = "Send authenticating key", Category = "Session", IsActiveByDefault = false, Order = 1)]
    public class SendAuthenticatingKeyMacro : SendAuthenticatingMacroBase
    {
        protected override string TemplateName => "Authenticating key";

        protected override AuthenticationScheme Scheme => AuthenticationScheme.Key;
    }
}