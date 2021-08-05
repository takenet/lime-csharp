using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Lime.Protocol;
using Lime.Transport.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.WebSockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using TransportType = Lime.Transport.AspNetCore.TransportType;

namespace Lime.Sample.AspNetCore
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        
        public void ConfigureServices(IServiceCollection services)
        {
            // Register WebSockets middleware
            services.AddWebSockets(options => { options.KeepAliveInterval = TimeSpan.FromSeconds(30); });
            
            // Register Lime
            services.AddLime(options =>
            {
                // Define the local server node address
                options.LocalNode = new Node("postmaster", "localhost", Environment.MachineName);
                
                // Load certificate for TLS support
                var serverCertificate = GetCertificate();

                // Set the listening endpoints
                options.EndPoints.Clear();
                options.EndPoints.Add(new TransportEndPoint()
                {
                    EndPoint = new IPEndPoint(IPAddress.Any, 55321),
                    Transport = TransportType.Tcp,
                    ServerCertificate = serverCertificate
                });
                options.EndPoints.Add(new TransportEndPoint()
                {
                    EndPoint = new IPEndPoint(IPAddress.Any, 8080),
                    Transport = TransportType.WebSocket,
                    ServerCertificate = serverCertificate
                });
                options.EndPoints.Add(new TransportEndPoint()
                {
                    EndPoint = new IPEndPoint(IPAddress.Any, 443),
                    Transport = TransportType.Http,
                    ServerCertificate = serverCertificate
                });
            });
            
            // Register ASP.NET Core MVC (optional)
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo {Title = "Lime.Sample.AspNetCore", Version = "v1"});
            });
        }
        
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Lime.Sample.AspNetCore v1"));
            }
            
            app.UseWebSockets();
            app.UseLime();

            // Conventional MVC configuration.
            // The MVC middleware can be reached if there an HTTP endpoint defined in Lime options. 
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
        
        private static X509Certificate2 GetCertificate()
        {
            // Note: This implementation is intended for development.
            // TODO: Retrieve a valid certificate.
            var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            var certs = store.Certificates.Find(X509FindType.FindBySubjectName, "localhost", true);

            if (certs.Count == 0)
            {
                throw new Exception("No localhost certificate found for TLS");
            }
            
            return certs[0];
        }
    }
}