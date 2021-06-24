using System;
using System.Linq;
using System.Reflection;
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
            services.Configure(configure);
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<KestrelServerOptions>, LimeOptionsSetup>());

            services.AddSingleton<IDocumentTypeResolver, DocumentTypeResolver>();
            services.AddSingleton<IEnvelopeSerializer, EnvelopeSerializer>();
            services.AddSingleton<TransportListener>();

            services.RegisterManyTransient<IMessageListener>();
            services.RegisterManyTransient<INotificationListener>();
            services.RegisterManyTransient<ICommandListener>();

            return services;
        }

        public static IApplicationBuilder UseLime(this IApplicationBuilder app)
        {
            app.UseMiddleware<LimeWebSocketMiddleware>();
            
            return app;
        }
        
        public static void RegisterManyTransient<T>(this IServiceCollection services)
        {
            var types = Assembly
                .GetEntryAssembly()
                ?.DefinedTypes
                .Where(t => !t.IsAbstract && typeof(T).IsAssignableFrom(t));

            if (types != null)
            {
                foreach (var type in types)
                {
                    services.AddTransient(typeof(T), type);
                }
            }
        }
    }
}