using Lime.Protocol;
using Lime.Transport.AspNetCore.Listeners;

namespace Lime.Transport.AspNetCore.UnitTests
{
    public class FakeCommandListener : FakeEnvelopeListener<Command>, ICommandListener
    {

    }
}