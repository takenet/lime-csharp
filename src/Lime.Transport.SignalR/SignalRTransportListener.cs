using System;
using System.Buffers;
using System.Collections.Generic;
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
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Lime.Transport.SignalR
{
    public sealed partial class SignalRTransportListener : ITransportListener, IDisposable
    {
        private const string UriSchemeHttps = "https";

        private readonly IHost _webHost;
        private readonly int _acceptCapacity;
        private readonly SemaphoreSlim _semaphore;
        private Channel<ITransport> _transportChannel;

        public SignalRTransportListener(Uri[] listenerUris,
            IEnvelopeSerializer envelopeSerializer,
            X509Certificate2 tlsCertificate = null,
            int bufferSize = 1000, // TODO
            ITraceWriter traceWriter = null,
            TimeSpan? keepAliveInterval = null,
            int acceptCapacity = -1,
            HttpProtocols httpProtocols = HttpProtocols.Http1AndHttp2,
            SslProtocols sslProtocols = SslProtocols.None,
            ArrayPool<byte> arrayPool = null,
            bool closeGracefully = true,
            Func<X509Certificate2, X509Chain, SslPolicyErrors, bool> clientCertificateValidationCallback = null)
        {
            ListenerUris = listenerUris;
            _acceptCapacity = acceptCapacity;
            _webHost = Host.CreateDefaultBuilder()
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

                                if (listenerUri.Scheme == UriSchemeHttps)
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
                        .AddSingleton(_transportChannel)
                        .AddSingleton(traceWriter)
                        .AddSingleton(envelopeSerializer)
                        .AddSignalR().AddHubOptions<EnvelopeHub>(hubOptions => { });
                    })
                    .Configure(app =>
                    {
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapHub<EnvelopeHub>("/envelope", options => { options.ApplicationMaxBufferSize = bufferSize; });
                        });
                    });
                })
                .Build();

            _semaphore = new SemaphoreSlim(1, 1);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "This is defined in the interface")]
        public Uri[] ListenerUris { get; }
        private bool IsStarted => _transportChannel != null && !_transportChannel.Reader.Completion.IsCompleted;

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

        public async Task<ITransport> AcceptTransportAsync(CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!IsStarted)
                {
                    throw new InvalidOperationException("The listener is not started");
                }

                return await _transportChannel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _semaphore.Release();
            }
        }

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

        public void Dispose()
        {
            _semaphore.Dispose();
            _webHost.Dispose();
        }
    }
}
