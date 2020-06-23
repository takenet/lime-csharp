using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Listeners;
using Lime.Protocol.Network;
using Lime.Protocol.Server;
using Lime.Transport.Tcp;
using Lime.Protocol.Security;
using Lime.Protocol.Serialization;
using Lime.Protocol.Serialization.Newtonsoft;
using Lime.Transport.WebSocket;

namespace Lime.Sample.Server
{
    class Program
    {
        private static readonly Node _serverNode = Node.Parse("server@domain.com/default");
        private static readonly INodeRegistry _nodeRegistry = new NodeRegistry();
        
        static async Task Main(string[] args)
        {
            Console.Write("Enter the listener URI (Press ENTER for default): ");

            var inputListenerUri = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(inputListenerUri))
            {
                inputListenerUri = "net.tcp://localhost:55321";
            }

            Console.WriteLine("Starting the server...");
            
            // Create and start a listener
            var listenerUri = new Uri(inputListenerUri);
            var transportListener = GetTransportListenerForUri(listenerUri);
            
            var server = new Protocol.Server.Server(
                transportListener,
                CreateServerChannel,
                new[] {SessionCompression.None},
                new[] {SessionEncryption.None, SessionEncryption.TLS},
                new[] {AuthenticationScheme.Guest},
                AuthenticateAsync,
                CreateChannelListener,
                _nodeRegistry
            );

            // Starts listening
            try
            {
                await server.StartAsync();
                Console.WriteLine("Server started. Press ENTER to stop.");
                Console.ReadLine();
                await server.StopAsync();
                Console.WriteLine("Server stopped. Press any key to exit.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Server failed with message '{ex.Message}'. Press any key to exit.");
            }

            Console.Read();
        }
        
        static ITransportListener GetTransportListenerForUri(Uri uri)
        {
            var serializer = new EnvelopeSerializer(new DocumentTypeResolver());

            switch (uri.Scheme)
            {
                case "net.tcp":
                    X509Certificate2 serverCertificate = null;  // You should provide a certificate for TLS
                    return new PipeTcpTransportListener(
                        uri,
                        serverCertificate,
                        serializer,
                        2048);
                case "ws":
                case "wss":
                    return new WebSocketTransportListener(
                        new[] { uri },                        
                        serializer);
                default:
                    throw new NotSupportedException($"Unsupported URI scheme '{uri.Scheme}'");
            }
        }

        private static IServerChannel CreateServerChannel(ITransport transport)
        {
            Console.WriteLine("Transport connection received.");
            
            // Creates a new server channel, setting the session parameters
            var sessionId = Guid.NewGuid();
            var sendTimeout = TimeSpan.FromSeconds(60);

            return new ServerChannel(
                sessionId.ToString(),
                _serverNode,
                transport,
                sendTimeout);
        }
        
        private static Task<AuthenticationResult> AuthenticateAsync(Identity identity, Authentication authentication, CancellationToken cancellationToken)
        {
            Console.WriteLine("Authenticating identity {0} with scheme {1}", identity, authentication.GetAuthenticationScheme());
            return new AuthenticationResult(DomainRole.Member).AsCompletedTask();
        }

        private static IChannelListener CreateChannelListener(IChannelInformation channelInformation)
        {
            var serverChannel = (IServerChannel)channelInformation;
            
            return new ChannelListener(
                (m, ct) => ConsumeMessageAsync(m, serverChannel, ct),
                (n, ct) => ConsumeNotificationAsync(n, serverChannel, ct),
                (c, ct) => ConsumeCommandAsync(c, serverChannel, ct));
        }
        
        private static async Task<bool> ConsumeMessageAsync(Message message, IServerChannel serverChannel, CancellationToken cancellationToken)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;

            // Check the destination of the envelope
            if (message.To == null ||
                message.To.Equals(_serverNode))
            {
                // Destination is the current node
                var notification = new Notification()
                {
                    Id = message.Id,
                    Event = Event.Received
                };

                await serverChannel.SendNotificationAsync(notification, CancellationToken.None);
                Console.WriteLine("Message with id '{0}' received from '{1}': {2}", message.Id, message.From ?? serverChannel.RemoteNode, message.Content);
            }
            else
            {
                var destinationServerChannel = await _nodeRegistry.GetAsync(message.To, cancellationToken);
                if (destinationServerChannel != null)
                {
                    // Destination is a node that has a session with the server
                    message.From = serverChannel.RemoteNode;
                    await destinationServerChannel.SendMessageAsync(message, CancellationToken.None);
                    Console.WriteLine("Message forwarded from '{0}' to '{1}'", serverChannel.RemoteNode, destinationServerChannel.RemoteNode);
                }
                else
                {
                    // Destination not found
                    var notification = new Notification()
                    {
                        Id = message.Id,
                        Event = Event.Failed,
                        Reason = new Reason()
                        {
                            Code = ReasonCodes.ROUTING_DESTINATION_NOT_FOUND,
                            Description = "Destination not found"
                        }
                    };

                    await serverChannel.SendNotificationAsync(notification, CancellationToken.None);
                    Console.WriteLine("Invalid message destination from '{0}': '{1}'", serverChannel.RemoteNode, message.To);
                }
            }
            
            return true;
        }
        
        private static async Task<bool> ConsumeNotificationAsync(Notification notification, IServerChannel serverChannel, CancellationToken cancellationToken)
        {
            Console.ForegroundColor = ConsoleColor.DarkBlue;

            // Check the destination of the envelope
            if (notification.To == null ||
                notification.To.Equals(_serverNode))
            {
                Console.WriteLine("Notification with id {0} received from '{1}' - Event: {2}", notification.Id, notification.From ?? serverChannel.RemoteNode, notification.Event);
            }
            else 
            {
                var destinationServerChannel = await _nodeRegistry.GetAsync(notification.To, cancellationToken);
                if (destinationServerChannel != null)
                {
                    // Destination is a node that has a session with the server
                    notification.From = serverChannel.RemoteNode;
                    await destinationServerChannel.SendNotificationAsync(notification, CancellationToken.None);

                }
            }

            return true;
        }
        
        private static async Task<bool> ConsumeCommandAsync(Command command, IServerChannel serverChannel, CancellationToken cancellationToken)
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            
            // Check the destination of the envelope
            if (command.To == null ||
                command.To.Equals(_serverNode))
            {
                // Destination is the current node
                var responseCommand = new Command()
                {
                    Id = command.Id,
                    Status = CommandStatus.Failure,
                    Reason = new Reason()
                    {
                        Code = ReasonCodes.COMMAND_RESOURCE_NOT_SUPPORTED,
                        Description = "The resource is not supported"
                    }
                };

                await serverChannel.SendCommandAsync(responseCommand, CancellationToken.None);
                Console.WriteLine("Command with id '{0}' received from '{1}' - Method: {2} - URI: {3}", command.Id, command.From ?? serverChannel.RemoteNode, command.Method, command.Uri);
            }
            else
            {
                var destinationServerChannel = await _nodeRegistry.GetAsync(command.To, cancellationToken);
                if (destinationServerChannel != null)
                {
                    // Destination is a node that has a session with the server
                    command.From = serverChannel.RemoteNode;
                    await destinationServerChannel.SendCommandAsync(command, CancellationToken.None);
                    Console.WriteLine("Command forwarded from '{0}' to '{1}'", serverChannel.RemoteNode,
                        destinationServerChannel.RemoteNode);
                }
                else
                {
                    // Destination not found
                    var responseCommand = new Command()
                    {
                        Id = command.Id,
                        Status = CommandStatus.Failure,
                        Reason = new Reason()
                        {
                            Code = ReasonCodes.ROUTING_DESTINATION_NOT_FOUND,
                            Description = "Destination not found"
                        }
                    };

                    await serverChannel.SendCommandAsync(responseCommand, CancellationToken.None);
                    Console.WriteLine("Invalid command destination from '{0}': '{1}'", serverChannel.RemoteNode,
                        command.To);
                }
            }

            return true;
        }
    }
}
