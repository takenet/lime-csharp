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
        
        /// <summary>
        /// The supported transport compression options.
        /// </summary>
        public SessionCompression[] EnabledCompressionOptions { get; set; } = {SessionCompression.None};
        
        /// <summary>
        /// The supported transport encryption options.
        /// </summary>
        public SessionEncryption[] EnabledEncryptionOptions { get; set; } = {SessionEncryption.None, SessionEncryption.TLS};
        
        /// <summary>
        /// The supported authentication schemes.
        /// </summary>
        public AuthenticationScheme[] SchemeOptions { get; set; } = {AuthenticationScheme.Guest};
        
        /// <summary>
        /// The handler for session authentication.
        /// It receives an <see cref="Identity"/> and <see cref="Authentication"/> objects and should return a <see cref="AuthenticationResult"/> with the operation result.
        /// For <see cref="TransportType.Http"/> endpoints, this handler is called on every request. 
        /// </summary>
        public AuthenticationHandler AuthenticationHandler { get; set; } = (identity, authentication, token) => Task.FromResult(new AuthenticationResult(DomainRole.Member));
        
        /// <summary>
        /// The handler for node registration.
        /// </summary>
        public RegistrationHandler RegistrationHandler { get; set; } = (candidate, channel, token) => Task.FromResult(new Node(Guid.NewGuid().ToString(), Environment.UserDomainName, Environment.MachineName));
    }
}