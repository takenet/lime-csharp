using Lime.Protocol;
using Lime.Transport.AspNetCore.Listeners;

namespace Lime.Transport.AspNetCore.UnitTests
{
    public class FakeNotificationListener : FakeEnvelopeListener<Notification>, INotificationListener
    {

    }
}