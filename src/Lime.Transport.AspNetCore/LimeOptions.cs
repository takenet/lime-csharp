using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Security;
using Lime.Protocol.Server;

namespace Lime.Transport.AspNetCore
{
    /// <summary>
    /// Defines configuration options for the Lime service. 
    /// </summary>
    public class LimeOptions
    {
        /// <summary>
        /// The endpoints for receiving connections.
        /// </summary>
        public ICollection<TransportEndPoint> EndPoints { get; set; } = new List<TransportEndPoint> 
        {
            new TransportEndPoint()
            {
                Transport = TransportType.Tcp,
                EndPoint = new IPEndPoint(IPAddress.Any, 55321)
            },
            new TransportEndPoint()
            {
                Transport = TransportType.WebSocket,
                EndPoint = new IPEndPoint(IPAddress.Any, 8080)
            },
            new TransportEndPoint()
            {
                Transport = TransportType.Http,
                EndPoint = new IPEndPoint(IPAddress.Any, 443)
            },
        };
        
        /// <summary>
        /// The node for the local server, which will be presented to the connected clients.
        /// </summary>
        public Node LocalNode { get; set; } = new Node(Environment.UserName, Environment.UserDomainName ?? "localhost", Environment.MachineName);
        public TimeSpan SendTimeout { get; set; } = TimeSpan.FromSeconds(60);
        public TimeSpan CloseTimeout { get; set; } = TimeSpan.FromSeconds(5);
        public SessionCompression[] EnabledCompressionOptions { get; set; } = {SessionCompression.None};
        public SessionEncryption[] EnabledEncryptionOptions { get; set; } = {SessionEncryption.None, SessionEncryption.TLS};
        public AuthenticationScheme[] SchemeOptions { get; set; } = {AuthenticationScheme.Guest};
        public AuthenticationHandler AuthenticationHandler { get; set; } = (identity, authentication, token) => Task.FromResult(new AuthenticationResult(DomainRole.Member));
        public RegistrationHandler RegistrationHandler { get; set; } = (candidate, channel, token) => Task.FromResult(new Node(Guid.NewGuid().ToString(), Environment.UserDomainName, Environment.MachineName));
    }
}