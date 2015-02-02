using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Configuration;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lime.Messaging.Contents;
using Lime.Protocol;
using Lime.Protocol.Client;
using Lime.Transport.Tcp;

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
            System.Console.Write("Host name (ENTER for default): ");

            var hostName = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(hostName))
            {
                hostName = Dns.GetHostName();
            }

            System.Console.Write("Port number (ENTER for default): ");

            var portNumberString = Console.ReadLine();
            int portNumber;

            if (!int.TryParse(portNumberString, out portNumber))
            {
                portNumber = 55321;
            }

            // Creates a new transport and connect to the server
            var serverUri = new Uri(string.Format("net.tcp://{0}:{1}", hostName, portNumber)); 
            var transport = new TcpTransport();
            await transport.OpenAsync(serverUri, CancellationToken.None);

            // Creates a new client channel
            var sendTimeout = TimeSpan.FromSeconds(60);

            var clientChannel = new ClientChannel(
                transport,
                sendTimeout);

            // TODO Support for negotiation and authentication
            var session = await clientChannel.EstablishSessionAsync(
                (compressionOptions) => compressionOptions.First(),     // Compression selector 
                (encryptionOptions) => encryptionOptions.First(),       // Encryption selector
                null,                                                   // Client identity
                (authenticationSchemes, roundtrip) => null,             // Authentication
                "default",
                CancellationToken.None);

            if (session.State == SessionState.Established)
            {
                var cts = new CancellationTokenSource();
                var consumeMessagesTask = ConsumeMessagesAsync(clientChannel, cts.Token).WithPassiveCancellation();
                var consumeCommandsTask = ConsumeCommandsAsync(clientChannel, cts.Token).WithPassiveCancellation();
                var consumeNotificationsTask = ConsumeNotificationsAsync(clientChannel, cts.Token).WithPassiveCancellation();

                var finishedSessionTask = clientChannel
                    .ReceiveFinishedSessionAsync(cts.Token)
                    .ContinueWith(t =>
                    {
                        Console.Write("The session was finished. ");
                        if (t.Result.Reason != null)
                        {
                            Console.Write("Reason: {0}", t.Result.Reason);
                        }
                        Console.WriteLine();
                    });

                Console.WriteLine("Session established. Id: {0} - Local node: {1} - Remote node: {2}", session.Id, session.To, session.From);

                while (clientChannel.State == SessionState.Established)
                {                    
                    Console.Write("Destination node (Type EXIT to quit): ");
                    var toInput = Console.ReadLine();
                    if (toInput != null && 
                        toInput.Equals("exit", StringComparison.InvariantCultureIgnoreCase))
                    {                        
                        break;
                    }
                    
                    Node to;
                    if (Node.TryParse(toInput, out to))
                    {
                        Console.Write("Message text: ");
                        var messageText = Console.ReadLine();

                        var message = new Message()
                        {
                            To = to,
                            Content = new PlainText()
                            {
                                Text = messageText
                            }
                        };

                        await clientChannel.SendMessageAsync(message);
                    }
                }

                Console.WriteLine("Finishing...");
                cts.Cancel();
                await Task.WhenAll(consumeMessagesTask, consumeCommandsTask, consumeNotificationsTask);
                await clientChannel.SendFinishingSessionAsync();
                await finishedSessionTask;
            }
            else
            {
                Console.Write("Could not establish the session. ");
                if (session.Reason != null)
                {
                    Console.Write("Reason: {0}", session.Reason);
                }
                Console.WriteLine();
            }

            Console.WriteLine("Press any key to exit.");
            Console.Read();
        }

        static async Task ConsumeMessagesAsync(IClientChannel clientChannel, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var message = await clientChannel.ReceiveMessageAsync(cancellationToken);
                Console.WriteLine("Message with id '{0}' received from '{1}': {2}", message.Id, message.From ?? clientChannel.RemoteNode, message.Content);
            }
        }

        static async Task ConsumeCommandsAsync(IClientChannel clientChannel, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var command = await clientChannel.ReceiveCommandAsync(cancellationToken);
                Console.WriteLine("Command with id '{0}' received from '{1}' - Method: {2} - URI: {3}", command.Id, command.From ?? clientChannel.RemoteNode, command.Method, command.Uri);
            }
        }

        static async Task ConsumeNotificationsAsync(IClientChannel clientChannel, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var notification = await clientChannel.ReceiveNotificationAsync(cancellationToken);
                Console.WriteLine("Notification with id {0} received from '{1}' - Event: {2}", notification.Id, notification.From ?? clientChannel.RemoteNode, notification.Event);
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
