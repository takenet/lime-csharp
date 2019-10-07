using System;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Lime.Protocol.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Lime.Transport.Http
{
    public class HttpTransportListener : ITransportListener
    {
        private readonly IWebHost _webHost;
        private readonly IEnvelopeSerializer _envelopeSerializer;
        private readonly ITraceWriter _traceWriter;
        private readonly int _acceptCapacity;

        private Channel<ITransport> _transportChannel;


        public HttpTransportListener(
            Uri[] listenerUris,            
            IEnvelopeSerializer envelopeSerializer,
            X509Certificate2 tlsCertificate = null,
            ITraceWriter traceWriter = null,
            int acceptCapacity = -1,
            HttpProtocols httpProtocols = HttpProtocols.Http1AndHttp2,
            SslProtocols sslProtocols = SslProtocols.Tls11 | SslProtocols.Tls12)
        {
            if (listenerUris == null) throw new ArgumentNullException(nameof(listenerUris));
            if (listenerUris.Length == 0)
            {
                throw new ArgumentException("At least one listener URI should be provided.", nameof(listenerUris));
            }
            if (listenerUris.Any(u => u.Scheme != Uri.UriSchemeHttp && u.Scheme != Uri.UriSchemeHttps))
            {
                throw new ArgumentException($"Invalid URI scheme. Should be '{Uri.UriSchemeHttp}' or '{Uri.UriSchemeHttps}'.", nameof(listenerUris));
            }
            if (tlsCertificate == null && listenerUris.Any(u => u.Scheme == Uri.UriSchemeHttps))            
            {
                throw new ArgumentException($"The certificate should be provided when listening to a '{Uri.UriSchemeHttps}' URI.", nameof(listenerUris));
            }

            ListenerUris = listenerUris;
            _envelopeSerializer = envelopeSerializer;
            _traceWriter = traceWriter;
            _acceptCapacity = acceptCapacity;

            _webHost = new WebHostBuilder()
                .UseKestrel(serverOptions =>
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

                            if (listenerUri.Scheme ==  Uri.UriSchemeHttps)
                            {                                                                
                                listenOptions.UseHttps(tlsCertificate, httpsOptions =>
                                {
                                    httpsOptions.SslProtocols = sslProtocols;
                                });
                            }
                        });
                    }
                    
                    serverOptions.AddServerHeader = false;
                })                
                .SuppressStatusMessages(true)                
                .Configure(app =>
                {
                    app.Run(ProcessHttpContextAsync);
                })
                .Build();
        }


        public Task StartAsync(CancellationToken cancellationToken)
        {
            _transportChannel = _acceptCapacity > 0
                ? Channel.CreateBounded<ITransport>(_acceptCapacity)
                : Channel.CreateUnbounded<ITransport>();
            
            return _webHost.StartAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            if (!IsStarted)
            {
                return Task.FromException(new InvalidOperationException("The listener is not started"));
            }
                        
            _transportChannel.Writer.Complete();
            
            return _webHost.StopAsync(cancellationToken);
        }

        public Uri[] ListenerUris { get; }
        
        public Task<ITransport> AcceptTransportAsync(CancellationToken cancellationToken)
        {
            if (!IsStarted)
            {
                return Task.FromException<ITransport>(new InvalidOperationException("The listener is not started"));
            }
            
            return _transportChannel.Reader.ReadAsync(cancellationToken).AsTask();
        }

        private bool IsStarted => _transportChannel != null && !_transportChannel.Reader.Completion.IsCompleted;
        
        
        private async Task ProcessHttpContextAsync(HttpContext context)
        {
            if (context.Request.Method == HttpMethods.Post)
            {
                
            }
            else if (context.Request.Method == HttpMethods.Get)
            {
                
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
            }
        }
    }

    internal class ServerHttpTransport : TransportBase
    {
        
        
        public ServerHttpTransport()
        {
            
        }

        public Channel<Envelope> InputChannel { get; }
        
        public Channel<Envelope> OutputChannel { get; }
        
        public override Task SendAsync(Envelope envelope, CancellationToken cancellationToken)
        {
            return OutputChannel.Writer.WriteAsync(envelope, cancellationToken).AsTask();
        }

        public override Task<Envelope> ReceiveAsync(CancellationToken cancellationToken)
        {
            return InputChannel.Reader.ReadAsync(cancellationToken).AsTask();
        }

        public override bool IsConnected => !InputChannel.Reader.Completion.IsCompleted && !OutputChannel.Reader.Completion.IsCompleted;
        
        protected override Task PerformOpenAsync(Uri uri, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
        
        protected override Task PerformCloseAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }


    }
}