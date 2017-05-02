using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Lime.Protocol.Server;
using SslCertBinding.Net;

namespace Lime.Transport.WebSocket
{
    /// <summary>
    /// Defines a websocket transport listener service.
    /// </summary>
    /// <seealso cref="Lime.Protocol.Server.ITransportListener" />
    public class WebSocketTransportListener : ITransportListener
    {
        public static readonly string UriSchemeWebSocket = "ws";
        public static readonly string UriSchemeWebSocketSecure = "wss";
        public static readonly Guid DefaultApplicationId = Guid.Parse("46754fc2-d8e2-4b41-a3f0-ed1878c77e59");
        public static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(30);

        private readonly X509CertificateInfo _tlsCertificate; 
        private readonly IEnvelopeSerializer _envelopeSerializer;
        private readonly ITraceWriter _traceWriter;
        private readonly int _bufferSize;
        private readonly bool _bindCertificateToPort;
        private readonly Guid _applicationId;
        private readonly TimeSpan _keepAliveInterval;
        private readonly HttpListener _httpListener;
        private readonly BufferBlock<HttpListenerContext> _httpListenerContextBufferBlock;
        private readonly TransformBlock<HttpListenerContext, ITransport> _httpListenerWebSocketContextTransformBlock;
        private readonly BufferBlock<ITransport> _transportBufferBufferBlock;
        private readonly ITargetBlock<ITransport> _nullTargetBlock;
        private CancellationTokenSource _acceptTransportCts;
        private Task _acceptTransportTask;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketTransportListener"/> class.
        /// </summary>
        /// <param name="listenerUri">The listener URI.</param>
        /// <param name="tlsCertificate">The SSL/TLS certificate information to be used in case of using the 'wss' scheme.</param>
        /// <param name="envelopeSerializer">The envelope serializer.</param>
        /// <param name="traceWriter">The trace writer.</param>
        /// <param name="bufferSize">Size of the buffer.</param>
        /// <param name="keepAliveInterval">The keep alive interval.</param>
        /// <param name="bindCertificateToPort">if set to <c>true</c> indicates that the provided certificate should be bound to the listener IP address.</param>
        /// <param name="applicationId">The application id for binding the certificate to the listene port.</param>
        /// <param name="acceptTransportBoundedCapacity">The number of concurrent transport connections that can be accepted in parallel.</param>
        /// <exception cref="ArgumentNullException">
        /// </exception>
        public WebSocketTransportListener(
            Uri listenerUri,
            X509CertificateInfo tlsCertificate,
            IEnvelopeSerializer envelopeSerializer, 
            ITraceWriter traceWriter = null,
            int bufferSize = 16384,
            TimeSpan? keepAliveInterval = null,
            bool bindCertificateToPort = true,
            Guid? applicationId = null,
            int acceptTransportBoundedCapacity = 10)
        {
            if (listenerUri == null) throw new ArgumentNullException(nameof(listenerUri));

            if (listenerUri.Scheme != UriSchemeWebSocket &&
                listenerUri.Scheme != UriSchemeWebSocketSecure)
            {
                throw new ArgumentException($"Invalid URI scheme. The expected value is '{UriSchemeWebSocket}' or '{UriSchemeWebSocketSecure}'.");
            }

            if (tlsCertificate == null &&
                listenerUri.Scheme == UriSchemeWebSocketSecure)
            {
                throw new ArgumentNullException(nameof(tlsCertificate));
            }

            ListenerUris = new[] { listenerUri };
            _tlsCertificate = tlsCertificate;
            _envelopeSerializer = envelopeSerializer ?? throw new ArgumentNullException(nameof(envelopeSerializer));
            _traceWriter = traceWriter;
            _bufferSize = bufferSize;
            _bindCertificateToPort = bindCertificateToPort;
            _applicationId = applicationId ?? DefaultApplicationId;
            _keepAliveInterval = keepAliveInterval ?? System.Net.WebSockets.WebSocket.DefaultKeepAliveInterval;
            _httpListener = new HttpListener();
            
            var boundedCapacity = new ExecutionDataflowBlockOptions()
            {
                BoundedCapacity = acceptTransportBoundedCapacity
            };

            // Create blocks
            _httpListenerContextBufferBlock = new BufferBlock<HttpListenerContext>(boundedCapacity);
            _httpListenerWebSocketContextTransformBlock = new TransformBlock<HttpListenerContext, ITransport>(
                c => AcceptWebSocketAsync(c), boundedCapacity);
            _transportBufferBufferBlock = new BufferBlock<ITransport>(boundedCapacity);
            _nullTargetBlock = DataflowBlock.NullTarget<ITransport>();

            // Link blocks
            _httpListenerContextBufferBlock.LinkTo(_httpListenerWebSocketContextTransformBlock);
            _httpListenerWebSocketContextTransformBlock.LinkTo(_transportBufferBufferBlock, t => t != null);
            _httpListenerWebSocketContextTransformBlock.LinkTo(_nullTargetBlock, t => t == null);
        }

        public Uri[] ListenerUris { get; }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_httpListener.IsListening) throw new InvalidOperationException("The listener is already active");
            var listenerUri = ListenerUris[0];
            var prefix = listenerUri.ToString();
            prefix = prefix
                .Replace($"{UriSchemeWebSocket}:", $"{Uri.UriSchemeHttp}:")
                .Replace($"{UriSchemeWebSocketSecure}:", $"{Uri.UriSchemeHttps}:")
                .Replace("://localhost", "://*");
            _httpListener.Prefixes.Add(prefix);

            if (_bindCertificateToPort &&
                _tlsCertificate != null &&
                listenerUri.Scheme.Equals(UriSchemeWebSocketSecure))
            {
                var ipPort = new IPEndPoint(IPAddress.Parse("0.0.0.0"), listenerUri.Port);
                var config = new CertificateBindingConfiguration();
                config.Bind(
                    new CertificateBinding(
                        _tlsCertificate.Thumbprint, _tlsCertificate.Store, ipPort, _applicationId));
            }

            _httpListener.Start();
            _acceptTransportCts?.Dispose();
            _acceptTransportCts = new CancellationTokenSource();
            _acceptTransportTask = Task.Run(AcceptTransportsAsync);
            return Task.CompletedTask;
        }

        public async Task<ITransport> AcceptTransportAsync(CancellationToken cancellationToken)
        {
            if (_acceptTransportTask == null) throw new InvalidOperationException("The listener is not active");
            if (_acceptTransportTask.IsCompleted)
            {
                await _acceptTransportTask.WithCancellation(cancellationToken).ConfigureAwait(false);
                throw new InvalidOperationException("The listener task is completed");
            }

            using (
                var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken,
                    _acceptTransportCts.Token))
            {
                return await _transportBufferBufferBlock.ReceiveAsync(linkedCts.Token).ConfigureAwait(false);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_acceptTransportTask == null) throw new InvalidOperationException("The listener is not active");
            _acceptTransportCts.Cancel();
            using (var cts = new CancellationTokenSource(StopTimeout))
            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken))
            {
                ITransport pendingTransport;
                while (_transportBufferBufferBlock.TryReceive(out pendingTransport))
                {
                    try
                    {
                        await pendingTransport.CloseAsync(linkedCts.Token).ConfigureAwait(false);
                    }
                    catch { }
                    finally
                    {
                        pendingTransport.DisposeIfDisposable();
                    }
                }
            }
            _httpListener.Stop();
            await _acceptTransportTask.ConfigureAwait(false);
        }

        /// <summary>
        /// Occurs when the listener loop raises an exception.
        /// </summary>
        public event EventHandler<ExceptionEventArgs> ListenerException;

        private async Task AcceptTransportsAsync()
        {
            while (!_acceptTransportCts.IsCancellationRequested)
            {
                try
                {
                    var httpContext = await _httpListener
                        .GetContextAsync()
                        .WithCancellation(_acceptTransportCts.Token)
                        .ConfigureAwait(false);

                    await _httpListenerContextBufferBlock
                        .SendAsync(httpContext, _acceptTransportCts.Token)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (_acceptTransportCts.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    ListenerException.RaiseEvent(this, new ExceptionEventArgs(ex));
                }
            }
        }

        private async Task<ITransport> AcceptWebSocketAsync(HttpListenerContext httpListenerContext)
        {
            try
            {
                if (!httpListenerContext.Request.IsWebSocketRequest)
                {
                    httpListenerContext.Response.StatusCode = 400;
                    httpListenerContext.Response.Close();
                    return null;
                }

                var context = await httpListenerContext.AcceptWebSocketAsync(
                    LimeUri.LIME_URI_SCHEME, _bufferSize, _keepAliveInterval)
                    .WithCancellation(_acceptTransportCts.Token)
                    .ConfigureAwait(false);

                return new ServerWebSocketTransport(context, _envelopeSerializer, _traceWriter, _bufferSize);
            }
            catch (OperationCanceledException) when (_acceptTransportCts.IsCancellationRequested) { }
            catch (Exception ex)
            {
                ListenerException.RaiseEvent(this, new ExceptionEventArgs(ex));
            }

            return null;
        }
    }
}
