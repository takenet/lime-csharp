using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Server;
using Lime.Transport.Tcp;
using Lime.Protocol.Security;
using Lime.Protocol.Serialization.Newtonsoft;
using Lime.Transport.Redis;
using Lime.Transport.WebSocket;
using StackExchange.Redis;

namespace Lime.Sample.Server
{
    class Program
    {
        static IDictionary<Node, IServerChannel> _nodeChannelsDictionary = new Dictionary<Node, IServerChannel>();
        static Node _serverNode = Node.Parse("server@domain.com/default");

        static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        static async Task MainAsync(string[] args)
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

            // Starts listening
            try
            {
                await transportListener.StartAsync();
                var cts = new CancellationTokenSource();
                var listenerTask = ListenAsync(transportListener, cts.Token);

                Console.WriteLine("Server started. Press ENTER to stop.");
                Console.ReadLine();
                cts.Cancel();

                await listenerTask;
                await transportListener.StopAsync();

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
            var serializer = new JsonNetSerializer();

            switch (uri.Scheme)
            {
                case "net.tcp":
                    X509Certificate2 serverCertificate = null;  // You should provide a certificate for TLS
                    return new TcpTransportListener(
                        uri,
                        serverCertificate,
                        serializer);
                case "ws":
                case "wss":
                    return new WebSocketTransportListener(
                        uri,
                        null,
                        serializer);

                case "redis":
                    return new RedisTransportListener(uri, serializer);

                default:
                    throw new NotSupportedException($"Unsupported URI scheme '{uri.Scheme}'");
            }
        }


        static async Task ListenAsync(ITransportListener transportListener, CancellationToken cancellationToken)
        {
            // List of all active consumer tasks
            var consumerTasks = new List<Task>();


            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Awaits for a new transport connection 
                    var transport = await transportListener.AcceptTransportAsync(cancellationToken);
                    Console.WriteLine("Transport connection received.");
                    await transport.OpenAsync(null, cancellationToken);

                    // Creates a new server channel, setting the session parameters
                    var sessionId = Guid.NewGuid();
                    var sendTimeout = TimeSpan.FromSeconds(60);

                    var serverChannel = new ServerChannel(
                        sessionId.ToString(),
                        _serverNode,
                        transport,
                        sendTimeout);

                    var consumerTask = Task.Run(async () => await ConsumeAsync(serverChannel, cancellationToken),
                        cancellationToken);

                    var continuation = consumerTask
                        .ContinueWith(t =>
                        {
                            if (t.Exception != null)
                            {
                                Console.WriteLine("Consumer task failed: {0}", t.Exception.InnerException.Message);
                            }

                            consumerTasks.Remove(consumerTask);
                        }, cancellationToken);

                    consumerTasks.Add(consumerTask);
                }
                catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("The listener failed with an error: {0}", ex);
                }
            }

            await Task.WhenAll(consumerTasks);
        }

        static async Task ConsumeAsync(IServerChannel serverChannel, CancellationToken cancellationToken)
        {
            try
            {
                await serverChannel.EstablishSessionAsync(
                    serverChannel.Transport.GetSupportedCompression(),
                    serverChannel.Transport.GetSupportedEncryption(),
                    new[] {AuthenticationScheme.Guest},
                    (identity, authentication) =>
                        new AuthenticationResult(null,
                            new Node()
                            {
                                Name = EnvelopeId.NewId(),
                                Domain = "limeprotocol.org",
                                Instance = Environment.MachineName
                            }),
                    cancellationToken);

                if (serverChannel.State == SessionState.Established)
                {
                    _nodeChannelsDictionary.Add(serverChannel.RemoteNode, serverChannel);

                    // Consume the channel envelopes
                    var consumeMessagesTask =
                        ConsumeMessagesAsync(serverChannel, cancellationToken).WithPassiveCancellation();
                    var consumeCommandsTask =
                        ConsumeCommandsAsync(serverChannel, cancellationToken).WithPassiveCancellation();
                    var consumeNotificationsTask =
                        ConsumeNotificationsAsync(serverChannel, cancellationToken).WithPassiveCancellation();
                    // Awaits for the finishing envelope
                    var finishingSessionTask = serverChannel.ReceiveFinishingSessionAsync(cancellationToken);

                    // Stops the consumer when any of the tasks finishes
                    await
                        Task.WhenAny(finishingSessionTask, consumeMessagesTask, consumeCommandsTask,
                            consumeNotificationsTask);

                    if (finishingSessionTask.IsCompleted)
                    {
                        await serverChannel.SendFinishedSessionAsync(CancellationToken.None);
                    }
                }

                if (serverChannel.State != SessionState.Finished &&
                    serverChannel.State != SessionState.Failed)
                {
                    await serverChannel.SendFailedSessionAsync(new Reason()
                    {
                        Code = ReasonCodes.SESSION_ERROR,
                        Description = "The session failed"
                    }, CancellationToken.None);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {

            }
            finally
            {
                if (serverChannel.RemoteNode != null)
                {
                    _nodeChannelsDictionary.Remove(serverChannel.RemoteNode);
                }

                serverChannel.DisposeIfDisposable();
            }
        }

        static async Task ConsumeMessagesAsync(IServerChannel serverChannel, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Console.ResetColor();

                var message = await serverChannel.ReceiveMessageAsync(cancellationToken);

                Console.ForegroundColor = ConsoleColor.DarkRed;

                IServerChannel destinationServerChannel;
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
                else if (_nodeChannelsDictionary.TryGetValue(message.To, out destinationServerChannel))
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
        }

        static async Task ConsumeCommandsAsync(IServerChannel serverChannel, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Console.ResetColor();

                var command = await serverChannel.ReceiveCommandAsync(cancellationToken);

                Console.ForegroundColor = ConsoleColor.DarkGreen;


                IServerChannel destinationServerChannel;
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
                else if (_nodeChannelsDictionary.TryGetValue(command.To, out destinationServerChannel))
                {
                    // Destination is a node that has a session with the server
                    command.From = serverChannel.RemoteNode;
                    await destinationServerChannel.SendCommandAsync(command, CancellationToken.None);
                    Console.WriteLine("Command forwarded from '{0}' to '{1}'", serverChannel.RemoteNode, destinationServerChannel.RemoteNode);
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
                    Console.WriteLine("Invalid command destination from '{0}': '{1}'", serverChannel.RemoteNode, command.To);
                }
            }
        }

        static async Task ConsumeNotificationsAsync(IServerChannel serverChannel, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Console.ResetColor();

                var notification = await serverChannel.ReceiveNotificationAsync(cancellationToken);

                Console.ForegroundColor = ConsoleColor.DarkBlue;

                IServerChannel destinationServerChannel;
                // Check the destination of the envelope
                if (notification.To == null ||
                    notification.To.Equals(_serverNode))
                {
                    Console.WriteLine("Notification with id {0} received from '{1}' - Event: {2}", notification.Id, notification.From ?? serverChannel.RemoteNode, notification.Event);
                }
                else if (_nodeChannelsDictionary.TryGetValue(notification.To, out destinationServerChannel))
                {
                    // Destination is a node that has a session with the server
                    notification.From = serverChannel.RemoteNode;
                    await destinationServerChannel.SendNotificationAsync(notification, CancellationToken.None);
                }
            }
        }
    }

    public static class TaskExtensions
    {
        public static Task WithPassiveCancellation(this Task task)
        {
            return task.ContinueWith(t => t, TaskContinuationOptions.OnlyOnCanceled);
        }
    }
}
