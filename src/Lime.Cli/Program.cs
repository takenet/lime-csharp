using CommandLine;
using CommandLine.Text;
using Lime.Messaging;
using Lime.Messaging.Resources;
using Lime.Protocol;
using Lime.Protocol.Client;
using Lime.Protocol.Listeners;
using Lime.Protocol.Network;
using Lime.Protocol.Network.Modules;
using Lime.Protocol.Security;
using Lime.Protocol.Serialization;
using Lime.Protocol.Serialization.Newtonsoft;
using Lime.Protocol.Util;
using Lime.Transport.Tcp;
using Lime.Transport.WebSocket;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Cli
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var parsedArgsResult = Parser.Default.ParseArguments<Options>(args);

            if (parsedArgsResult.Tag == ParserResultType.NotParsed)
            {
                HelpText.AutoBuild(parsedArgsResult);
                return;
            }

            Console.WriteLine("Welcome to LIME");

            var options = ((Parsed<Options>)parsedArgsResult).Value;

            IOnDemandClientChannel channel;

            using (var cts = CreateCancellationTokenSource(options.Timeout))
            {
                channel = await ConnectAsync(options.ToConnectionInformation(), cts.Token);
            }

            Console.WriteLine("Connected to the server");

            while (true)
            {
                Console.Write("> ");
                var input = Console.ReadLine();

                if (string.IsNullOrEmpty(input)) break;
            }

            using (var cts = CreateCancellationTokenSource(options.Timeout))
            {
                await channel.FinishAsync(cts.Token);
            }

            Console.WriteLine("Bye!");
        }

        private static CancellationTokenSource CreateCancellationTokenSource(int timeout)
        {
            return new CancellationTokenSource(TimeSpan.FromSeconds(timeout));
        }

        private static async Task<IOnDemandClientChannel> ConnectAsync(ConnectionInformation connectionInformation, CancellationToken cancellationToken)
        {
            ITransport transportFactory() => CreateTransportForUri(connectionInformation.ServerUri);

            // Creates a new client channel
            var builder = ClientChannelBuilder
                .Create(transportFactory, connectionInformation.ServerUri)
                .AddBuiltHandler((channel, handlerCancellationToken) =>
                {
                    channel.CommandModules.Add(new ReplyPingChannelModule(channel));
                    return Task.CompletedTask;
                })
                .CreateEstablishedClientChannelBuilder()
                .WithEncryption(SessionEncryption.None)
                .WithInstance(connectionInformation.Instance)
                .AddEstablishedHandler(async (channel, handlerCancellationToken) =>
                {
                    await channel.SetResourceAsync(
                        new LimeUri(UriTemplates.PRESENCE),
                        connectionInformation.Presence,
                        handlerCancellationToken);
                })
                .AddEstablishedHandler(async (channel, handlerCancellationToken) =>
                {
                    await channel.SetResourceAsync(
                        new LimeUri(UriTemplates.RECEIPT),
                        connectionInformation.Receipt,
                        handlerCancellationToken);
                });

            if (connectionInformation.Identity == null)
            {
                builder = builder.WithAuthentication<GuestAuthentication>();
            }
            else
            {
                builder = builder
                    .WithIdentity(connectionInformation.Identity)
                    .WithPlainAuthentication(connectionInformation.Password);
            }

            var clientChannel = new OnDemandClientChannel(builder);

            //var clientChannel2 = new MultiplexerClientChannel()

            clientChannel.ChannelCreationFailedHandlers.Add(information =>
            {
                Console.Write("Could not establish the session: {0}", information.Exception.Message);
                Console.WriteLine();
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
                    Console.WriteLine("Command with id '{0}' received from '{1}' - Method: {2} - URI: {3}", command.Id, command.GetSender(), command.Method, command.Uri);
                    Console.ResetColor();
                    return TaskUtil.TrueCompletedTask;
                });


            await clientChannel.EstablishAsync(CancellationToken.None);
            channelListener.Start(clientChannel);

            return clientChannel;
        }

        private static ITransport CreateTransportForUri(Uri uri)
        {
            switch (uri.Scheme)
            {
                case "net.tcp":
                    return new TcpTransport(
                        new EnvelopeSerializer(
                            new DocumentTypeResolver().WithMessagingDocuments()));
                case "ws":
                case "wss":
                    return new ClientWebSocketTransport(
                        new EnvelopeSerializer(
                            new DocumentTypeResolver().WithMessagingDocuments()));

                default:
                    throw new NotSupportedException($"Unsupported URI scheme '{uri.Scheme}'");
            }
        }
    }
}
