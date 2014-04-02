using Lime.Protocol.Network;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
            
            var compression = SessionCompression.GZip;
            var encryption = SessionEncryption.TLS;

            await target.SendNegotiatingSessionAsync(compression, encryption);

            _transport.Verify(
                t => t.SendAsync(It.Is<Session>(
                        e => e.State == SessionState.Negotiating &&
                             e.Id == target.SessionId &&
                             e.Compression == compression &&
                             e.Encryption == encryption),
                    It.IsAny<CancellationToken>()),
                    Times.Once());
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task SendNegotiatingSessionAsync_NewState_ThrowsInvalidOperationException()
        {
            var target = GetTarget(SessionState.New);

            var compression = SessionCompression.GZip;
            var encryption = SessionEncryption.TLS;

            await target.SendNegotiatingSessionAsync(compression, encryption);
        }

        #endregion


        #region SendMessageAsync

        [TestMethod]
        public async Task SendMessageAsync_EstablishedState_CallsTransport()
        {
            var target = GetTarget(SessionState.Established);

            var message = DataUtil.CreateMessage();
            message.Content = DataUtil.CreateTextContent();

            await target.SendMessageAsync(message);

            _transport.Verify(
                t => t.SendAsync(It.Is<Message>(
                        e => e.Id == message.Id &&
                             e.From.Equals(message.From) &&
                             e.To.Equals(message.To) &&
                             e.Content == message.Content),
                    It.IsAny<CancellationToken>()),
                    Times.Once());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task SendMessageAsync_NullMessage_ThrowsArgumentNullException()
        {
            var target = GetTarget(SessionState.Established);

            Message message = null;

            await target.SendMessageAsync(message);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task SendMessageAsync_NewState_ThrowsInvalidOperationException()
        {
            var target = GetTarget(SessionState.New);

            var message = DataUtil.CreateMessage();
            message.Content = DataUtil.CreateTextContent();

            await target.SendMessageAsync(message);
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
