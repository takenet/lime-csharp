using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Lime.Messaging.Contents;
using Lime.Messaging.Resources;
using Lime.Protocol;
using Lime.Protocol.Client;
using Lime.Protocol.Listeners;
using Lime.Protocol.Network;
using Lime.Protocol.Network.Modules;
using Lime.Transport.Tcp;
using Lime.Protocol.Security;
using Lime.Protocol.Serialization.Newtonsoft;
using Lime.Protocol.Util;

namespace Lime.Sample.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        static async Task MainAsync(string[] args)
        {
            Console.Write("Server URI (ENTER for default): ");

            var serverUriValue = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(serverUriValue))
            {
                serverUriValue = $"net.tcp://{Dns.GetHostName()}:{55321}";
            }

            Console.Write("Identity (name@domain - ENTER for none): ");

            Identity identity;
            if (!Identity.TryParse(Console.ReadLine(), out identity))
            {
                identity = null;
            }

            string password = null;

            if (identity != null)
            {
                Console.Write("Password: ");
                password = Console.ReadLine();
            }

            var setPresence = false;
            var setReceipts = false;

            // Creates a new transport and connect to the server
            var serverUri = new Uri(serverUriValue);
            Func<ITransport> transportFactory = () => CreateTransportForUri(serverUri);

            // Creates a new client channel
            var builder = ClientChannelBuilder
                .Create(transportFactory, serverUri)                
                .AddBuiltHandler((channel, token) =>
                {
                    channel.CommandModules.Add(new ReplyPingChannelModule(channel));
                    return TaskUtil.CompletedTask;
                })
                .CreateEstablishedClientChannelBuilder()
                .WithEncryption(SessionEncryption.None)
                .AddEstablishedHandler(async (c, t) =>
                {
                    if (setPresence)
                    {
                        await c.SetResourceAsync(
                            new LimeUri(UriTemplates.PRESENCE),
                            new Presence()
                            {
                                Status = PresenceStatus.Available,
                                RoutingRule = RoutingRule.Identity,
                                RoundRobin = true
                            },
                            t);
                    }
                })
                .AddEstablishedHandler(async (c, t) =>
                {
                    if (setReceipts)
                    {
                        await c.SetResourceAsync(
                            new LimeUri(UriTemplates.RECEIPT),
                            new Receipt()
                            {
                                Events = new[] {Event.Received, Event.Consumed}
                            },
                            t);
                    }
                });

            if (identity == null)
            {
                builder = builder.WithAuthentication<GuestAuthentication>();
            }
            else
            {
                builder = builder
                    .WithIdentity(identity)
                    .WithPlainAuthentication(password);
            }

            //var onDemandChannel = new OnDemandClientChannel(builder);
            var onDemandChannel = new MultiplexerClientChannel(builder);

            var running = true;
            onDemandChannel.ChannelCreationFailedHandlers.Add(information =>
            {
                Console.Write("Could not establish the session: {0}", information.Exception.Message);
                Console.WriteLine();
                running = false;
                return TaskUtil.FalseCompletedTask;
            });


            var channelListener = new ChannelListener(message =>
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("Message with id '{0}' received from '{1}': {2}", message.Id, message.GetSender(),
                    message.Content);
                Console.ResetColor();
                return TaskUtil.TrueCompletedTask;
            },
                notification =>
                {
                    Console.ForegroundColor = ConsoleColor.DarkBlue;
                    Console.WriteLine("Notification with id {0} received from '{1}' - Event: {2}",
                        notification.Id, notification.GetSender(), notification.Event);
                    Console.ResetColor();
                    return TaskUtil.TrueCompletedTask;
                },
                command =>
                {
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.WriteLine("Command with id '{0}' received from '{1}' - Method: {2} - URI: {3}", command.Id,
                        command.GetSender(), command.Method, command.Uri);
                    Console.ResetColor();
                    return TaskUtil.TrueCompletedTask;
                });


            await onDemandChannel.EstablishAsync(CancellationToken.None);
            channelListener.Start(onDemandChannel);

            while (running)
            {
                Console.Write("Destination node (Type EXIT to quit): ");
                var toInput = Console.ReadLine();
                if (toInput != null &&
                    toInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                Node to = null;
                if (string.IsNullOrEmpty(toInput) || Node.TryParse(toInput, out to))
                {
                    Console.Write("Message text: ");
                    var text = Console.ReadLine();

                    var stopwatch = Stopwatch.StartNew();

                    Console.Write("Number of times to send (ENTER to 1): ");
                    int count;
                    if (!int.TryParse(Console.ReadLine(), out count))
                    {
                        count = 1;
                    }

                    await Task.WhenAll(
                        Enumerable
                            .Range(0, count)
                            .Select(async i =>
                            {
                                var message = new Message
                                {
                                    Id = i.ToString(),
                                    To = to,
                                    Content = new PlainText
                                    {
                                        Text = text
                                    }
                                };

                                await onDemandChannel.SendMessageAsync(message, CancellationToken.None);
                            }));

                    stopwatch.Stop();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Elapsed: {0} ms", stopwatch.ElapsedMilliseconds);
                    Console.ResetColor();
                }

            }

            channelListener.Stop();
            await Task.WhenAll(
                channelListener.MessageListenerTask,
                channelListener.NotificationListenerTask,
                channelListener.CommandListenerTask);

            await onDemandChannel.FinishAsync(CancellationToken.None);

            Console.WriteLine("Press any key to exit.");
            Console.Read();
        }

        private static ITransport CreateTransportForUri(Uri uri)
        {
            switch (uri.Scheme)
            {
                case "net.tcp":
                    return new TcpTransport(new JsonNetSerializer(), bufferSize: 1024, traceWriter: new DebugTraceWriter());
                //case "ws":
                //case "wss":
                //    return new ClientWebSocketTransport(new JsonNetSerializer(), new DebugTraceWriter());
                //case "redis":
                //    return new RedisTransport(uri, new JsonNetSerializer());

                default:
                    throw new NotSupportedException($"Unsupported URI scheme '{uri.Scheme}'");
            }
        }

        static async Task ConsumeMessagesAsync(IClientChannel clientChannel, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var message = await clientChannel.ReceiveMessageAsync(cancellationToken);

                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("Message with id '{0}' received from '{1}': {2}", message.Id, message.From ?? clientChannel.RemoteNode, message.Content);
                Console.ResetColor();
            }
        }

        static async Task ConsumeCommandsAsync(IClientChannel clientChannel, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var command = await clientChannel.ReceiveCommandAsync(cancellationToken);
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine("Command with id '{0}' received from '{1}' - Method: {2} - URI: {3}", command.Id, command.From ?? clientChannel.RemoteNode, command.Method, command.Uri);
                Console.ResetColor();
            }
        }

        static async Task ConsumeNotificationsAsync(IClientChannel clientChannel, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var notification = await clientChannel.ReceiveNotificationAsync(cancellationToken);

                Console.ForegroundColor = ConsoleColor.DarkBlue;
                Console.WriteLine("Notification with id {0} received from '{1}' - Event: {2}", notification.Id, notification.From ?? clientChannel.RemoteNode, notification.Event);
                Console.ResetColor();
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
