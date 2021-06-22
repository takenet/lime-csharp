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
            foreach (var (protocol, endPoint) in _options.EndPoints)
            {
                options.Listen(endPoint, builder =>
                {
                    builder.UseConnectionLogging();

                    switch (protocol)
                    {
                        case "wss":
                            builder.UseHttps();
                            break;

                        case "net.tcp":
                            builder.UseConnectionHandler<LimeConnectionHandler>();
                            break;
                        
                        case "ws":
                            break;
                        
                        default:
                            throw new NotSupportedException($"Unsupported protocol '{protocol}'");
                    }
                });
            }
        }
    }
}