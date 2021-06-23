using System;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;

namespace Lime.Transport.AspNetCore
{
    internal sealed class LimeOptionsSetup : IConfigureOptions<KestrelServerOptions>
    {
        private readonly LimeOptions _options;

        public LimeOptionsSetup(IOptions<LimeOptions> options)
        {
            _options = options.Value;
        }
        
        public void Configure(KestrelServerOptions options)
        {
            foreach (var endPoint in _options.EndPoints)
            {
                options.Listen(endPoint.EndPoint, builder =>
                {
                    builder.UseConnectionLogging();

                    switch (endPoint.Transport)
                    {
                        case TransportType.Tcp:
                            builder.UseConnectionHandler<LimeConnectionHandler>();
                            break;
                        
                        case TransportType.Ws:
                            if (endPoint.Tls)
                            {
                                builder.UseHttps(httpsOptions =>
                                {
                                    httpsOptions.ServerCertificate = endPoint.ServerCertificate;
                                });
                            }
                            break;

                        case TransportType.Http:
                        default:
                            throw new NotSupportedException($"Unsupported tran '{endPoint.Transport}'");
                    }
                });
            }
        }
    }
}