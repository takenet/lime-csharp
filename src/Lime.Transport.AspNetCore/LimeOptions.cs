using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
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
        public RegistrationHandler RegistrationHandler { get; set; } = (candidate, channel, token) => Task.FromResult<Node?>(new Node(Guid.NewGuid().ToString(), Environment.UserDomainName, Environment.MachineName));

        /// <summary>
        /// The handler for node unregistration.
        /// </summary>
        public UnregistrationHandler UnregistrationHandler { get; set; } = (node, channel, token) => Task.CompletedTask;
        
        /// <summary>
        /// The channel send timeout.
        /// Each send operation must be completed in the specified timeout or it will be canceled.
        /// </summary>
        public TimeSpan SendTimeout { get; set; } = TimeSpan.FromSeconds(60);
        
        /// <summary>
        /// The channel close timeout. 
        /// </summary>
        public TimeSpan CloseTimeout { get; set; } = TimeSpan.FromSeconds(5);
        
        /// <summary>
        /// The number of envelopes to be buffered internally by the channel in the receive operations.
        /// If this limit is reached, the channel will not consume the transport until the buffer is consumed by the receive operations.
        /// </summary>
        public int EnvelopeBufferSize { get; set; } = 1;
        
        /// <summary>
        /// Automatically fill missing envelope 'from', 'to' and 'pp' properties using channel information.
        /// </summary>
        public bool FillEnvelopeRecipients { get; set; } = false;
        
        /// <summary>
        /// Indicates if the channel should reply automatically to ping request commands.
        /// In this case, the ping command are not returned by the ReceiveCommandAsync method.
        /// </summary>
        public bool AutoReplyPings { get; set; } = true;
        
        /// <summary>
        /// The interval to ping the remote party.
        /// </summary>
        public TimeSpan? RemotePingInterval { get; set; }
        
        /// <summary>
        /// The timeout to close the channel due to inactivity.
        /// </summary>
        public TimeSpan? RemoteIdleTimeout { get; set; }
        
        /// <summary>
        /// The channel consume timeout.
        /// Each envelope received from the transport must be consumed in the specified interval or it will cause the channel to be closed.
        /// </summary>
        public TimeSpan? ConsumeTimeout { get; set; }

        
    }
}