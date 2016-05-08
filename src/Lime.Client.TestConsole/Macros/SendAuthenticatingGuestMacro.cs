using Lime.Protocol.Security;

namespace Lime.Client.TestConsole.Macros
{
    [Macro(Name = "Send authenticating guest", Category = "Session", IsActiveByDefault = false, Order = 1)]
    public class SendAuthenticatingGuestMacro : SendAuthenticatingMacroBase
    {
        protected override string TemplateName => "Authenticating guest";

        protected override AuthenticationScheme Scheme => AuthenticationScheme.Guest;
    }
}
