using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Lime.Protocol.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Tcp
{
    public class TcpTransportListener : ITransportListener
    {
        #region Private Fields

        private IEnvelopeSerializer _envelopeSerializer;
        private ITraceWriter _traceWriter;
        private TcpListener _tcpListener;
        private X509Certificate2 _sslCertificate;
        private bool _isListening;

        #endregion

        #region Constructor

        public TcpTransportListener(X509Certificate2 sslCertificate, IEnvelopeSerializer envelopeSerializer, ITraceWriter traceWriter = null)
        {
            if (sslCertificate != null)
            {
                if (!_sslCertificate.HasPrivateKey)
                {
                    throw new ArgumentException("The certificate must have a private key");
                }

                try
                {
                    // Checks if the private key is available for the current user
                    var key = sslCertificate.PrivateKey;
                }
                catch (CryptographicException ex)
                {
                    throw new SecurityException("The current user doesn't have access to the certificate private key. Use WinHttpCertCfg.exe to assign the necessary permissions.", ex);
                }
            }

            _sslCertificate = sslCertificate;
            _envelopeSerializer = envelopeSerializer;
            _traceWriter = traceWriter;
        }

        #endregion

        #region ITransportListener Members

        /// <summary>
        /// Start listening connections
        /// at the specified URI
        /// </summary>
        /// <param name="listenerUri"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">listenerUri</exception>
        /// <exception cref="System.ArgumentException">
        /// Invalid URI scheme. The expected value is 'net.tcp'.
        /// or
        /// Could not resolve the IPAddress of the hostname
        /// </exception>
        /// <exception cref="System.InvalidOperationException">The listener is already active</exception>
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

                if (dnsEntry.AddressList.Any(a => a.AddressFamily == AddressFamily.InterNetwork))
                {
                    listenerEndPoint = new IPEndPoint(dnsEntry.AddressList.First(a => a.AddressFamily == AddressFamily.InterNetwork), listenerUri.Port);
                }
                else
                {
                    throw new ArgumentException("Could not resolve the IPAddress of the hostname");
                }                
            }
            
            _tcpListener = new TcpListener(listenerEndPoint);
            _tcpListener.Start();

            _isListening = true;
        }

        /// <summary>
        /// Accepts a new transport connection
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">The listener was not started. Calls StartAsync first.</exception>
        public async Task<ITransport> AcceptTransportAsync(CancellationToken cancellationToken)
        {
            if (!_isListening)
            {
                throw new InvalidOperationException("The listener was not started. Calls StartAsync first.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            var tcpClient = await _tcpListener.AcceptTcpClientAsync().ConfigureAwait(false);

            return new TcpTransport(
                new TcpClientAdapter(tcpClient),
                _envelopeSerializer,
                serverCertificate: _sslCertificate,
                traceWriter: _traceWriter);            
        }

        /// <summary>
        /// Stops the tranport listener
        /// </summary>
        /// <returns></returns>
        public Task StopAsync()
        {
            _isListening = false;

            _tcpListener.Stop();
            _tcpListener = null;

            return Task.FromResult<object>(null);
        }

        #endregion
    }
}
