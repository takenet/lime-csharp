using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Lime.Protocol.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Lime.Transport.SignalR
{
    /// <summary>
    /// Implements the listener interface for receiving transport connections using SignalR as the underlying transport mechanism.
    /// </summary>
    /// <inheritdoc cref="ITransportListener"/>
    /// <remarks>This type is thread safe.</remarks>
    public sealed partial class SignalRTransportListener : ITransportListener, IDisposable
    {
        private const int DEFAULT_MAX_BUFFER_SIZE = 32768;

        private readonly IHost _webHost;
        private readonly int _acceptCapacity;
        private readonly int _backpressureLimit;
        private readonly SemaphoreSlim _semaphore;
        private Channel<ITransport> _transportChannel;

        /// <summary>
        /// Initializes a new instance of the <see cref="SignalRTransportListener"/> class that listens on the specied URIs with the provided options.
        /// </summary>
        /// <param name="listenerUris">The URIs which will be listened to.</param>
        /// <param name="envelopeSerializer">The serializer for envelopes exchanged in the transport connections.</param>
        /// <param name="tlsCertificate">The certificates used when listening on HTTPS. Default <c>null</c>.</param>
        /// <param name="maxBufferSize">The maximum size in bytes for the underlying buffer. Default <c>32768</c>.</param>
        /// <param name="traceWriter">A sink for tracing messages. Default <c>null</c>.</param>
        /// <param name="keepAliveInterval">The interval used by the server to send keep alive pings to connected clientes. Default <c>null</c>.</param>
        /// <param name="acceptCapacity">The maximum capacity for new connections. Unlimited when set to zero or less. Default <c>0</c>.</param>
        /// <param name="httpProtocols">The HTTP protocols that will be enabled on the endpoints. Default <see cref="HttpProtocols.Http1AndHttp2"/></param>
        /// <param name="sslProtocols">Allowable SSL protcols. Default <see cref="SslProtocols.None"/>.</param>
        /// <param name="boundedCapacity">The limit after which requests in a given transport will start to get queued. Unlimited when set to zero or less. Default <c>0</c>.</param>
        /// <param name="clientCertificateValidationCallback">A callback for additional client certificate validation that will be invoked during authentication. Default <c>null</c>.</param>
        public SignalRTransportListener(
            Uri[] listenerUris,
            IEnvelopeSerializer envelopeSerializer,
            X509Certificate2 tlsCertificate = null,
            int maxBufferSize = DEFAULT_MAX_BUFFER_SIZE,
            ITraceWriter traceWriter = null,
            TimeSpan? keepAliveInterval = null,
            int acceptCapacity = 0,
            HttpProtocols httpProtocols = HttpProtocols.Http1AndHttp2,
            SslProtocols sslProtocols = SslProtocols.None,
            int boundedCapacity = 0,
            Func<X509Certificate2, X509Chain, SslPolicyErrors, bool> clientCertificateValidationCallback = null)
        {
            ListenerUris = listenerUris;
            _acceptCapacity = acceptCapacity;
            _backpressureLimit = boundedCapacity;
            _webHost = BuildWebHost(envelopeSerializer, tlsCertificate, maxBufferSize, traceWriter, httpProtocols, sslProtocols, clientCertificateValidationCallback, keepAliveInterval);

            _semaphore = new SemaphoreSlim(1, 1);
        }

        /// <inheritdoc />
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "This is defined in the interface")]        
        public Uri[] ListenerUris { get; }

        private bool IsStarted => _transportChannel != null && !_transportChannel.Reader.Completion.IsCompleted;

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">Thrown when the listener has already been started.</exception>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (IsStarted)
                {
                    throw new InvalidOperationException("The listener has already been started");
                }

                _transportChannel = _acceptCapacity > 0
                    ? Channel.CreateBounded<ITransport>(_acceptCapacity)
                    : Channel.CreateUnbounded<ITransport>();

                await _webHost.StartAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">Thrown when the listener is not started.</exception>
        public async Task<ITransport> AcceptTransportAsync(CancellationToken cancellationToken)
        {
            if (!IsStarted)
            {
                throw new InvalidOperationException("The listener is not started");
            }

            return await _transportChannel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">Thrown when the listener is not started.</exception>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!IsStarted)
                {
                    throw new InvalidOperationException("The listener is not started");
                }

                _transportChannel.Writer.Complete();

                await _webHost.StopAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private IHost BuildWebHost(
            IEnvelopeSerializer envelopeSerializer,
            X509Certificate2 tlsCertificate,
            int bufferSize,
            ITraceWriter traceWriter,
            HttpProtocols httpProtocols,
            SslProtocols sslProtocols,
            Func<X509Certificate2, X509Chain, SslPolicyErrors, bool> clientCertificateValidationCallback,
            TimeSpan? keepAliveInterval)
        {
            HttpConnectionDispatcherOptions httpConnectionDispatcherOptions = null;
            HubOptions hubOptions = null;

            return Host.CreateDefaultBuilder()
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        webBuilder.UseKestrel(serverOptions =>
                        {
                            foreach (var listenerUri in ListenerUris)
                            {
                                if (!IPAddress.TryParse(listenerUri.Host, out var ipAddress))
                                {
                                    ipAddress = IPAddress.Any;
                                }

                                var endPoint = new IPEndPoint(ipAddress, listenerUri.Port);
                                serverOptions.Listen(endPoint, listenOptions =>
                                {
                                    listenOptions.Protocols = httpProtocols;

                                    if (listenerUri.Scheme == Uri.UriSchemeHttps)
                                    {
                                        listenOptions.UseHttps(tlsCertificate, httpsOptions =>
                                        {
                                            httpsOptions.SslProtocols = sslProtocols;
                                            httpsOptions.ClientCertificateValidation = clientCertificateValidationCallback;
                                        });
                                    }
                                });
                            }

                            serverOptions.AddServerHeader = false;
                        })
                        .SuppressStatusMessages(true)
                        .ConfigureServices(services =>
                        {
                            services
                                .AddLogging()
                                .AddSingleton(sp => _transportChannel)
                                .AddSingleton(sp => httpConnectionDispatcherOptions)
                                .AddSingleton(sp => hubOptions)
                                .AddSingleton(envelopeSerializer)
                                .AddSingleton(new EnvelopeHubOptions { BoundedCapacity = _backpressureLimit })
                                .AddSingleton(new ConcurrentDictionary<string, Channel<string>>())
                                .AddSingleton<IUserIdProvider, RandomUserIdProvider>()
                                .AddSignalR().AddHubOptions<EnvelopeHub>(options =>
                                {
                                    hubOptions = options;
                                    options.KeepAliveInterval = keepAliveInterval;
                                });

                            if (traceWriter != null)
                                services.AddSingleton(traceWriter);

                        })
                        .Configure(app =>
                        {
                            app.UseRouting().UseEndpoints(endpoints =>
                            {
                                endpoints.MapHub<EnvelopeHub>("/envelope", options =>
                                {
                                    httpConnectionDispatcherOptions = options;
                                    options.TransportMaxBufferSize = bufferSize;
                                });
                            });
                        });
                    })
                    .Build();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _semaphore.Dispose();
            _webHost.Dispose();
        }
    }
}
