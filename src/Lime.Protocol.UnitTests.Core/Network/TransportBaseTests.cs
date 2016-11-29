using System;
using System.Linq;
using NUnit.Framework;
using Lime.Protocol.Network;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using Shouldly;

namespace Lime.Protocol.UnitTests.Network
{
    [TestFixture]
    public class TransportBaseTests
    {
        private TestTransportBase GetTarget()
        {
            return new TestTransportBase();
        }

        [Test]
        [Category("CloseAsync")]
        public async Task CloseAsync_Default_RaisesClosingAndCallsPerformCloseAndRaisesClosed()
        {
            var closingRaised = false;
            var closedRaised = false;

            var target = GetTarget();

            target.Closing += (sender, e) => closingRaised = true;
            target.Closed += (sender, e) => closedRaised = true;

            var cancellationToken = CancellationToken.None;
            await target.CloseAsync(cancellationToken);

            Assert.IsTrue(closingRaised);
            Assert.IsTrue(target.PerformCloseAsyncInvoked);
            Assert.IsTrue(target.PerformCloseAsynCancellationToken == cancellationToken);
            Assert.IsTrue(closedRaised);
        }

        [Test]
        [Category("GetSupportedCompression")]
        public void GetSupportedCompression_Default_GetsSessionCompressionNone()
        {
            var target = GetTarget();

            var supportedCompression = target.GetSupportedCompression();

            Assert.IsTrue(supportedCompression.Length == 1);
            Assert.IsTrue(supportedCompression.Contains(SessionCompression.None));
        }

        #region SetCompressionAsync

        [Test]
        [Category("SetCompressionAsync")]
        public async Task SetCompressionAsync_NoneCompression_SetsProperty()
        {
            var target = GetTarget();

            var compression = SessionCompression.None;
            var cancellationToken = CancellationToken.None;

            await target.SetCompressionAsync(compression, cancellationToken);

            Assert.IsTrue(target.Compression == compression);
        }

        [Test]
        [Category("SetCompressionAsync")]
        public void SetCompressionAsync_GZipCompression_ThrowsNotSupportedException()
        {
            // Arrange
            var target = GetTarget();
            var compression = SessionCompression.GZip;
            var cancellationToken = CancellationToken.None;

            // Act
            Should.Throw<NotSupportedException>(() => target.SetCompressionAsync(compression, cancellationToken));
        }

        #endregion


        [Test]
        [Category("GetSupportedEncryption")]
        public void GetSupportedEncryption_Default_GetsSessionEncryptionNone()
        {
            var target = GetTarget();

            var supportedEncryption = target.GetSupportedEncryption();

            Assert.IsTrue(supportedEncryption.Length == 1);
            Assert.IsTrue(supportedEncryption.Contains(SessionEncryption.None));
        }

        #region SetEncryptionAsync

        [Test]
        [Category("SetEncryptionAsync")]
        public async Task SetEncryptionAsync_NoneEncryption_SetsProperty()
        {
            var target = GetTarget();

            var encryption = SessionEncryption.None;
            var cancellationToken = CancellationToken.None;

            await target.SetEncryptionAsync(encryption, cancellationToken);

            Assert.IsTrue(target.Encryption == encryption);
        }

        [Test]
        [Category("SetEncryptionAsync")]
        public void SetEncryptionAsync_TLSEncryption_ThrowsNotSupportedException()
        {
            // Arrange
            var target = GetTarget();
            var encryption = SessionEncryption.TLS;
            var cancellationToken = CancellationToken.None;

            // Act
            Should.Throw<NotSupportedException>(() =>
                target.SetEncryptionAsync(encryption, cancellationToken));
        }

        #endregion

        #region OnClosingAsync

        [Test]
        [Category("OnClosingAsync")]
        public async Task OnClosingAsync_AnyException_RaisesClosing()
        {
            var target = GetTarget();
            bool closingRaised = false;

            target.Closing += (sender, e) => closingRaised = true;

            await target.CallsOnClosingAsync();

            Assert.IsTrue(closingRaised);
        }

        [Test]
        [Category("OnClosingAsync")]
        public async Task OnClosingAsync_MultipleSubscribersOnClosingEvent_AwaitsForDeferral()
        {
            var target = GetTarget();
            bool closingSubscriber1Raised = false;
            bool closingSubscriber2Raised = false;


            target.Closing += async (sender, e) =>
            {
                using (e.GetDeferral())
                {
                    await Task.Delay(100);
                    closingSubscriber1Raised = true;
                }
            };

            target.Closing += async (sender, e) =>
            {
                using (e.GetDeferral())
                {
                    await Task.Delay(100);
                    closingSubscriber2Raised = true;
                }
            };

            await target.CallsOnClosingAsync();

            Assert.IsTrue(closingSubscriber1Raised);
            Assert.IsTrue(closingSubscriber2Raised);
        }

        #endregion

        #region OnClosedAsync

        [Test]
        [Category("OnClosed")]
        public void OnClosed_AnyException_RaisesClosed()
        {
            var target = GetTarget();
            bool closedRaised = false;

            target.Closed += (sender, e) => closedRaised = true;

            target.CallsOnClosed();

            Assert.IsTrue(closedRaised);
        }

        #endregion

        #region Private classes

        private class TestTransportBase : TransportBase
        {
            private Queue<Envelope> _buffer;

            public TestTransportBase(Queue<Envelope> buffer = null)
            {
                _buffer = buffer;
            }

            public bool SendAsyncInvoked { get; private set; }

            public bool OpenAsyncInvoked { get; private set; }

            public bool ReceiveAsyncInvoked { get; private set; }

            public bool PerformCloseAsyncInvoked { get; private set; }

            public CancellationToken PerformCloseAsynCancellationToken { get; private set; }

            public override bool IsConnected => OpenAsyncInvoked && !PerformCloseAsyncInvoked;

            public override Task SendAsync(Envelope envelope, CancellationToken cancellationToken)
            {
                this.SendAsyncInvoked = true;
                return Task.FromResult<object>(null);
            }

            public override Task<Envelope> ReceiveAsync(CancellationToken cancellationToken)
            {
                this.ReceiveAsyncInvoked = true;
                return Task.FromResult(_buffer.Dequeue());
            }


            protected override Task PerformOpenAsync(Uri uri, CancellationToken cancellationToken)
            {
                this.OpenAsyncInvoked = true;
                return Task.FromResult<object>(null);
            }

            protected override Task PerformCloseAsync(CancellationToken cancellationToken)
            {
                this.PerformCloseAsyncInvoked = true;
                this.PerformCloseAsynCancellationToken = cancellationToken;
                return Task.FromResult<object>(null);
            }


            public Task CallsOnClosingAsync()
            {
                return base.OnClosingAsync();
            }

            public void CallsOnClosed()
            {
                base.OnClosed();
            }

            public void CallsOnEnvelopeReceived(Envelope envelope)
            {
                //base.OnEnvelopeReceived(envelope);
            }


        }

        #endregion
    }
}
