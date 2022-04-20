using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Serialization;
using Lime.Transport.AspNetCore.Transport;
using Lime.Transport.Tcp;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Options;
using TcpClientAdapter = Lime.Transport.AspNetCore.Transport.TcpClientAdapter;

namespace Lime.Transport.AspNetCore.Middlewares
{
    internal class TcpConnectionHandler : ConnectionHandler
    {
        private readonly TransportListener _listener;
        private readonly IEnvelopeSerializer _envelopeSerializer;
        private readonly Dictionary<int, TransportEndPoint> _portEndPoints;

        public TcpConnectionHandler(
            TransportListener listener,
            IEnvelopeSerializer envelopeSerializer,
            IOptions<LimeOptions> options)
        {
            _listener = listener;
            _envelopeSerializer = envelopeSerializer;
            _portEndPoints = options
                .Value
                .EndPoints
                .Where(e => e.Transport == TransportType.Tcp)
                .ToDictionary(e => e.EndPoint.Port, e => e);
        }

        public override async Task OnConnectedAsync(ConnectionContext connection)
        {
            var tcpClient = new TcpClientAdapter(connection);

            if (connection.LocalEndPoint == null || !_portEndPoints.TryGetValue(((IPEndPoint)connection.LocalEndPoint).Port, out var transportEndPoint))
            {
                // This should never occur, but handling anyway.
                await connection.DisposeAsync();
                return;
            }

            using var transport = new TcpTransport(tcpClient: tcpClient,
                envelopeSerializer: _envelopeSerializer,
                serverCertificate: transportEndPoint.ServerCertificate);

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