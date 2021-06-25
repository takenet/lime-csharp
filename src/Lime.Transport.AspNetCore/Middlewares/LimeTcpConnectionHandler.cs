using System;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Serialization;
using Lime.Transport.Tcp;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Options;

namespace Lime.Transport.AspNetCore
{
    internal class LimeTcpConnectionHandler : ConnectionHandler
    {
        private readonly TransportListener _listener;
        private readonly IEnvelopeSerializer _envelopeSerializer;
        private readonly IOptions<LimeOptions> _options;
        private X509Certificate2? _serverCertificate;

        public LimeTcpConnectionHandler(
            TransportListener listener,
            IEnvelopeSerializer envelopeSerializer,
            IOptions<LimeOptions> options)
        {
            _listener = listener;
            _envelopeSerializer = envelopeSerializer;
            _options = options;
        }
        
        public override async Task OnConnectedAsync(ConnectionContext connection)
        {
            var tcpClient = new ConnectionContextTcpClientAdapter(connection);
            
            var transportEndPoint =
                _options.Value.EndPoints.FirstOrDefault(e =>
                    e.Transport == TransportType.Tcp && 
                    e.EndPoint.Port == ((IPEndPoint)connection.LocalEndPoint).Port);

            using var transport = new TcpTransport(tcpClient, _envelopeSerializer, transportEndPoint?.ServerCertificate);
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