using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Lime.Protocol.Network;
using Moq;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace Lime.Protocol.UnitTests.Network
{
    [TestClass]
    public class TransportBaseTests
    {
        private TestTransportBase GetTarget()
        {
            return new TestTransportBase();
        }

        [TestMethod]
        [TestCategory("CloseAsync")]
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

        [TestMethod]
        [TestCategory("GetSupportedCompression")]
        public void GetSupportedCompression_Default_GetsSessionCompressionNone()
        {
            var target = GetTarget();

            var supportedCompression = target.GetSupportedCompression();

            Assert.IsTrue(supportedCompression.Length == 1);
            Assert.IsTrue(supportedCompression.Contains(SessionCompression.None));
        }

        #region SetCompressionAsync

        [TestMethod]
        [TestCategory("SetCompressionAsync")]
        public async Task SetCompressionAsync_NoneCompression_SetsProperty()
        {
            var target = GetTarget();

            var compression = SessionCompression.None;
            var cancellationToken = CancellationToken.None;

            await target.SetCompressionAsync(compression, cancellationToken);

            Assert.IsTrue(target.Compression == compression);
        }

        [TestMethod]
        [TestCategory("SetCompressionAsync")]
        [ExpectedException(typeof(NotSupportedException))]
        public async Task SetCompressionAsync_GZipCompression_ThrowsNotSupportedException()
        {
            var target = GetTarget();

            var compression = SessionCompression.GZip;
            var cancellationToken = CancellationToken.None;

            await target.SetCompressionAsync(compression, cancellationToken);
        }

        #endregion


        [TestMethod]
        [TestCategory("GetSupportedEncryption")]
        public void GetSupportedEncryption_Default_GetsSessionEncryptionNone()
        {
            var target = GetTarget();

            var supportedEncryption = target.GetSupportedEncryption();

            Assert.IsTrue(supportedEncryption.Length == 1);
            Assert.IsTrue(supportedEncryption.Contains(SessionEncryption.None));
        }

        #region SetEncryptionAsync

        [TestMethod]
        [TestCategory("SetEncryptionAsync")]
        public async Task SetEncryptionAsync_NoneEncryption_SetsProperty()
        {
            var target = GetTarget();

            var encryption = SessionEncryption.None;
            var cancellationToken = CancellationToken.None;

            await target.SetEncryptionAsync(encryption, cancellationToken);

            Assert.IsTrue(target.Encryption == encryption);
        }

        [TestMethod]
        [TestCategory("SetEncryptionAsync")]
        [ExpectedException(typeof(NotSupportedException))]
        public async Task SetEncryptionAsync_TLSEncryption_ThrowsNotSupportedException()
        {
            var target = GetTarget();

            var encryption = SessionEncryption.TLS;
            var cancellationToken = CancellationToken.None;

            await target.SetEncryptionAsync(encryption, cancellationToken);
        }

        #endregion

        #region OnFailedAsync

        [TestMethod]
        [TestCategory("OnFailedAsync")]
        public async Task OnFailedAsync_AnyException_RaisesFailed()
        {
            var target = GetTarget();
            bool failedRaised = false;

            Exception exception = DataUtil.CreateException();

            target.Failed += (sender, e) => failedRaised = e.Exception == exception;

            await target.CallsOnFailedAsync(exception);

            Assert.IsTrue(failedRaised);
        }

        [TestMethod]
        [TestCategory("OnFailedAsync")]
        public async Task OnFailedAsync_MultipleSubscribersOnFailedEvent_AwaitsForDeferral()
        {
            var target = GetTarget();
            bool failedSubscriber1Raised = false;
            bool failedSubscriber2Raised = false;

            Exception exception = DataUtil.CreateException();

            target.Failed += async (sender, e) =>
                {
                    using (e.GetDeferral())
                    {
                        await Task.Delay(100);
                        failedSubscriber1Raised = true;
                    }
                };

            target.Failed += async (sender, e) =>
            {
                using (e.GetDeferral())
                {
                    await Task.Delay(100);
                    failedSubscriber2Raised = true;
                }
            };

            await target.CallsOnFailedAsync(exception);

            Assert.IsTrue(failedSubscriber1Raised);
            Assert.IsTrue(failedSubscriber2Raised);
        }

        #endregion

        #region OnClosingAsync

        [TestMethod]
        [TestCategory("OnClosingAsync")]
        public async Task OnClosingAsync_AnyException_RaisesClosing()
        {
            var target = GetTarget();
            bool closingRaised = false;

            target.Closing += (sender, e) => closingRaised = true;

            await target.CallsOnClosingAsync();

            Assert.IsTrue(closingRaised);
        }

        [TestMethod]
        [TestCategory("OnClosingAsync")]
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

        [TestMethod]
        [TestCategory("OnClosed")]
        public void OnClosed_AnyException_RaisesClosed()
        {
            var target = GetTarget();
            bool closedRaised = false;

            target.Closed += (sender, e) => closedRaised = true;

            target.CallsOnClosed();

            Assert.IsTrue(closedRaised);
        }

        #endregion

        #region OnEnvelopeReceived

        [TestMethod]
        [TestCategory("OnEnvelopeReceived")]
        public void OnEnvelopeReceived_AnyEnvelope_RaisesEnvelopeReceived()
        {
            var target = GetTarget();
            bool envelopeReceivedRaised = false;

            var text = DataUtil.CreateTextContent();
            var envelope = DataUtil.CreateMessage(text);

            target.EnvelopeReceived += (sender, e) => envelopeReceivedRaised = true;
            target.CallsOnEnvelopeReceived(envelope);

            Assert.IsTrue(envelopeReceivedRaised);
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


            public override Task OpenAsync(Uri uri, CancellationToken cancellationToken)
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

            public Task CallsOnFailedAsync(Exception ex)
            {
                return base.OnFailedAsync(ex);
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
                base.OnEnvelopeReceived(envelope);
            }


        }

        #endregion
    }
}
