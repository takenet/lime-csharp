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

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Registering ASP.NET WebSockets
            services.AddWebSockets(options => { options.KeepAliveInterval = TimeSpan.FromSeconds(30); });
            
            // Register Lime types
            services.AddLime(options =>
            {
                // Define the local server node address
                options.LocalNode = new Node("postmaster", "localhost", Environment.MachineName);
                
                // Load certificate for TLS support
                var serverCertificate = GetLocalhostCertificate();
                
                //options.AuthenticationHandler
                
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
            
            // MVC configurations (optional)
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo {Title = "Lime.Sample.AspNetCore", Version = "v1"});
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
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

            // Conventional MVC registration.
            // The MVC middleware can be reached if there an HTTP endpoint defined in Lime options. 
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
        
        private static X509Certificate2? GetLocalhostCertificate()
        {
            var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            var certs = store.Certificates.Find(X509FindType.FindBySubjectName, "localhost", true);

            X509Certificate2? serverCertificate = certs.Count > 0 ? certs[0] : null;
            return serverCertificate;
        }
    }
}