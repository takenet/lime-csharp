using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Lime.Protocol.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Tcp
{
    public class TcpTransportListener : ITransportListener
    {
        private IEnvelopeSerializer _envelopeSerializer;
        private ITraceWriter _traceWriter;

        private TcpListener _tcpListener;
        private X509Certificate _sslCertificate;        
        private bool _isListening;
        private Task _acceptTcpClientTask;

        public TcpTransportListener(IEnvelopeSerializer envelopeSerializer, ITraceWriter traceWriter = null)
        {
            _envelopeSerializer = envelopeSerializer;
            _traceWriter = traceWriter;
        }

        #region ITransportListener Members

        public async Task StartAsync(Uri listenerUri)
        {
            if (listenerUri == null)
            {
                throw new ArgumentNullException("listenerUri");
            }      

            if (listenerUri.Scheme != Uri.UriSchemeNetTcp)
            {
                throw new ArgumentException("Invalid URI scheme. The expected value is 'net.tcp'.");
            }

            if (_tcpListener != null)
            {
                throw new InvalidOperationException("The listener is already active");
            }

            IPEndPoint listenerEndPoint;

            if (listenerUri.IsLoopback)
            {
                listenerEndPoint = new IPEndPoint(IPAddress.Any, listenerUri.Port);
            }
            else
            {
                var dnsEntry = await Dns.GetHostEntryAsync(listenerUri.Host);

                if (dnsEntry.AddressList.Any())
                {
                    listenerEndPoint = new IPEndPoint(dnsEntry.AddressList.First(), listenerUri.Port);
                }
                else
                {
                    throw new ArgumentException("Could not resolve the IPAddress of the hostname");
                }                
            }
            
            _tcpListener = new TcpListener(listenerEndPoint);
            _tcpListener.Start();

            _isListening = true;
                
            _acceptTcpClientTask = this.ConnectAsync(CancellationToken.None)
                .ContinueWith(t =>
                {
                    if (t.Exception != null)
                    {

                    }
                });
        }

        public event EventHandler<TransportEventArgs> Connected;

        public Task StopAsync()
        {
            _isListening = false;

            _tcpListener.Stop();
            _tcpListener = null;

            return Task.FromResult<object>(null);
        }

        #endregion


        private async Task ConnectAsync(CancellationToken cancellationToken)
        {
            while (_isListening)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var tcpClient = await _tcpListener.AcceptTcpClientAsync();

                var transport = new TcpTransport(
                    tcpClient,
                    _envelopeSerializer,
                    sslCertificate: _sslCertificate,
                    traceWriter: _traceWriter
                    );

                this.Connected.RaiseEvent(this, new TransportEventArgs(transport));
            }

        }
    }
}
