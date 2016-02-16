using System;
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
            Console.Write("Host name (ENTER for default): ");

            var hostName = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(hostName))
            {
                hostName = Dns.GetHostName();
            }

            Console.Write("Port number (ENTER for default): ");

            int portNumber;
            if (!int.TryParse(Console.ReadLine(), out portNumber))
            {
                portNumber = 55321;
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
            
            // Creates a new transport and connect to the server
            var serverUri = new Uri($"net.tcp://{hostName}:{portNumber}"); 
            var transport = new TcpTransport(traceWriter: new DebugTraceWriter());

            // Creates a new client channel
            var builder = ClientChannelBuilder
                .Create(transport, serverUri)
                .AddBuiltHandler((channel, token) =>
                {
                    channel.CommandModules.Add(new ReplyPingChannelModule(channel));
                    return TaskUtil.CompletedTask;
                })
                .CreateEstablishedClientChannelBuilder()
                .AddEstablishedHandler(async (c, t) =>
                    await c.SetResourceAsync(
                        new LimeUri(UriTemplates.PRESENCE),
                        new Presence()
                        {
                            Status = PresenceStatus.Available
                        },
                        t))
                .AddEstablishedHandler(async (c, t) =>
                    await c.SetResourceAsync(
                        new LimeUri(UriTemplates.RECEIPT),
                        new Receipt()
                        {
                            Events = new[] { Event.Received, Event.Dispatched  }
                        },
                        t));

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
                                    
            var onDemandChannel = new OnDemandClientChannel(builder);
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

            channelListener.Start(onDemandChannel);

            while (running)
            {
                Console.Write("Destination node (Type EXIT to quit): ");
                var toInput = Console.ReadLine();
                if (toInput != null &&
                    toInput.Equals("exit", StringComparison.InvariantCultureIgnoreCase))
                {
                    break;
                }

                Node to = null;
                if (string.IsNullOrEmpty(toInput) || Node.TryParse(toInput, out to))
                {
                    Console.Write("Message text: ");
                    var message = new Message
                    {
                        To = to,
                        Content = new PlainText
                        {
                            Text = Console.ReadLine()
                        }
                    };

                    await onDemandChannel.SendMessageAsync(message);
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
