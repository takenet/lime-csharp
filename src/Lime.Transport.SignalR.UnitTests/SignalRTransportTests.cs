using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Lime.Protocol.Server;
using Lime.Protocol.UnitTests;
using Lime.Protocol.UnitTests.Common.Network;
using Microsoft.AspNetCore.SignalR.Client;
using NUnit.Framework;
using Shouldly;

namespace Lime.Transport.SignalR.UnitTests
{
    [TestFixture]
    public class SignalRTransportTests : TransportTestsBase
    {
        protected override ITransport CreateClientTransport(IEnvelopeSerializer envelopeSerializer) => new ClientSignalRTransport(envelopeSerializer);

        protected override Uri CreateListenerUri() => new Uri("http://localhost:57813");

        protected override ITransportListener CreateTransportListener(Uri uri, IEnvelopeSerializer envelopeSerializer)
            => new SignalRTransportListener(new[] { uri }, envelopeSerializer, traceWriter: TraceWriter);

        [Test]
        [Category("LocalEndPoint")]
        public override async Task LocalEndPoint_ConnectedTransport_ShouldEqualsClientRemoteEndPoint()
        {
            // Arrange
            var (clientTransport, serverTransport) = await GetAndOpenTargetsAsync();

            // Act
            var actual = serverTransport.LocalEndPoint;

            // Assert
            var expected = clientTransport.RemoteEndPoint;
            actual.ShouldBeOneOf(expected, "");
        }

        [Test]
        [Category("RemoteEndPoint")]
        public override async Task RemoteEndPoint_ConnectedTransport_ShouldEqualsClientLocalEndPoint()
        {
            // Arrange
            var (clientTransport, serverTransport) = await GetAndOpenTargetsAsync();

            // Act
            var actual = serverTransport.RemoteEndPoint;

            // Assert
            var expected = clientTransport.LocalEndPoint;
            actual.ShouldBeOneOf(expected, "");
        }

        [Test]
        [Category("SendAsync")]
        public async Task SendAsync_AfterServerDisconnect_ClientAutomaticallyReconnects()
        {
            ITransport clientTransport = null;
            ITransport serverTransport = null;
            ITransportListener transportListener = null;

            try
            {
                // Arrange
                transportListener = CreateTransportListener(ListenerUri, EnvelopeSerializer);
                await transportListener.StartAsync(CancellationToken);
                clientTransport = CreateClientTransport(EnvelopeSerializer);
                var serverTransportTask = transportListener.AcceptTransportAsync(CancellationToken);
                await clientTransport.OpenAsync(ListenerUri, CancellationToken);
                serverTransport = await serverTransportTask;
                await serverTransport.OpenAsync(ListenerUri, CancellationToken);
                var message = Dummy.CreateMessage(Dummy.CreateTextContent());

                // Act
                await transportListener.StopAsync(CancellationToken);
                await serverTransport.CloseAsync(CancellationToken);
                transportListener = CreateTransportListener(ListenerUri, EnvelopeSerializer);
                await transportListener.StartAsync(CancellationToken);
                serverTransport = await transportListener.AcceptTransportAsync(CancellationToken);
                await serverTransport.OpenAsync(ListenerUri, CancellationToken);

                await clientTransport.SendAsync(message, CancellationToken);
                var actual = await serverTransport.ReceiveAsync(CancellationToken);

                // Assert
                var actualMessage = actual.ShouldBeOfType<Message>();
                CompareMessages(message, actualMessage);
            }
            finally
            {
                (clientTransport as IDisposable)?.Dispose();
                (serverTransport as IDisposable)?.Dispose();
                (transportListener as IDisposable)?.Dispose();
            }
        }

    }
}
