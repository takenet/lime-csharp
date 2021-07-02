using Lime.Protocol;
using Lime.Transport.AspNetCore.Listeners;

namespace Lime.Transport.AspNetCore.UnitTests.Middlewares
{
    public class FakeCommandListener : FakeEnvelopeListener<Command>, ICommandListener
    {

    }
}