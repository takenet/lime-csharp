using Lime.Protocol.Network;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.UnitTests.Network
{
    [TestClass]
    public class ChannelBaseTests
    {
        private Mock<ITransport> _transport;
        private TimeSpan _sendTimeout;

        public ChannelBaseTests()
        {
            _transport = new Mock<ITransport>();
            _sendTimeout = TimeSpan.FromSeconds(30);
        }

        public ChannelBase GetTarget(SessionState state)
        {
            return new TestChannel(
                state,
                _transport.Object,
                _sendTimeout
                );
        }

        #region SendNegotiatingSessionAsync

        [TestMethod]
        public async Task SendNegotiatingSessionAsync_NegotiatingState_CallsTransport()
        {
            var target = GetTarget(SessionState.Negotiating);

        }

        #endregion

        private class TestChannel : ChannelBase
        {
            public TestChannel(SessionState state, ITransport transport, TimeSpan sendTimeout)
                : base(transport, sendTimeout)
            {
                base.State = state;
            }
        }
    }
}
