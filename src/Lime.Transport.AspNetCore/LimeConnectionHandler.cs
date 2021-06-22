using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Serialization;
using Lime.Transport.Tcp;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR;

namespace Lime.Transport.AspNetCore
{
    public class LimeConnectionHandler : ConnectionHandler
    {
        private readonly TransportListener _listener;
        private readonly IEnvelopeSerializer _envelopeSerializer;
        private X509Certificate2? _certificate;

        public LimeConnectionHandler(
            TransportListener listener,
            IEnvelopeSerializer envelopeSerializer)
        {
            _listener = listener;
            _envelopeSerializer = envelopeSerializer;
        }
        
        public override async Task OnConnectedAsync(ConnectionContext connection)
        {
            var tcpClient = new ConnectionContextTcpClientAdapter(connection);

            using var transport = new TcpTransport(tcpClient, _envelopeSerializer, _certificate);
            
            await transport.OpenAsync(null, default);
            
            try
            {
                await _listener.ListenAsync(transport, connection.ConnectionClosed);
            }
            finally
            {
                if (transport.IsConnected)
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    await transport.CloseAsync(cts.Token);
                }
            }
        }
    }
}