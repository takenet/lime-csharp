using System;
using Lime.Protocol.Serialization;
using Lime.Protocol.Serialization.Newtonsoft;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Lime.Transport.AspNetCore
{
    public static class LimeExtensions
    {
        public static IServiceCollection AddLime(this IServiceCollection services, Action<LimeOptions> configure)
        {
            services.Configure(nameof(Lime), configure);
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<KestrelServerOptions>, LimeOptionsSetup>());

            services.AddSingleton<IDocumentTypeResolver, DocumentTypeResolver>();
            services.AddSingleton<IEnvelopeSerializer, EnvelopeSerializer>();
            services.AddSingleton<TransportListener>();
            return services;
        }

        public static IApplicationBuilder UseLime(this IApplicationBuilder app)
        {
            app.UseMiddleware<LimeWebSocketMiddleware>();
            
            return app;
        }
    }
}