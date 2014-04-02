using Lime.Protocol.Client;
using Lime.Protocol.Network;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.UnitTests
{
    public static class SetupExtensions
    {
        public static void ReceiveEnvelope(this Mock<ITransport> transport, Envelope envelope)
        {
            transport.Raise(
                t => t.EnvelopeReceived += (sender, e) => { },
                new EnvelopeEventArgs<Envelope>(envelope));
        }

        public static void SetState(this IClientChannel channel, Mock<ITransport> transport, SessionState state)
        {
            var session = DataUtil.CreateSession();
            session.State = state;
            transport.ReceiveEnvelope(session);
        }
    }
}
