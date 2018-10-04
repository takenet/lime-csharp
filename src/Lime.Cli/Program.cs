using CommandLine;
using CommandLine.Text;
using Lime.Cli.Actions;
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
using SimpleInjector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Cli
{
    class Program
    {
        private static readonly Parser Parser = new Parser((settings) =>
        {
            settings.IgnoreUnknownArguments = true;
            settings.CaseInsensitiveEnumValues = true;
        });

        static async Task<int> Main(string[] args)
        {
            var parsedArgsResult = Parser.ParseArguments<Options>(args);
            if (parsedArgsResult.Tag == ParserResultType.NotParsed)
            {
                var helpText = HelpText.AutoBuild(parsedArgsResult);
                WriteInfo(helpText.ToString());
                return -1;
            }

            var options = ((Parsed<Options>)parsedArgsResult).Value;
            var interactive = string.IsNullOrWhiteSpace(options.Action);

            string[] actionArgs = null;
            if (!interactive)
            {
                // Action mode
                actionArgs = GetActionArgs(options.Action, args);
            }

            if (interactive)
            {
                WriteInfo("Welcome to LIME");
            }

            IOnDemandClientChannel channel;

            using (var cts = CreateCancellationTokenSource(options.Timeout))
            {
                try
                {
                    channel = await EstablishChannelAsync(options.ToConnectionInformation(), cts.Token);
                }
                catch (Exception ex)
                {
                    WriteError(ex.Message);
                    return -1;
                }
            }

            if (interactive)
            {
                WriteInfo("Channel established");
            }

            var actionsDictionary = GetActions();

            var resultStatus = 0;

            if (interactive)
            {
                await ExecuteInteractiveModeAsync(options, channel, actionsDictionary);
            }
            else if (!await ExecuteActionAsync(actionArgs, channel, actionsDictionary, options.Timeout))
            {
                resultStatus = -1;
            }

            using (var cts = CreateCancellationTokenSource(options.Timeout))
            {
                try
                {
                    await channel.FinishAsync(cts.Token);
                }
                catch (Exception ex)
                {
                    WriteError(ex.Message);
                    return -1;
                }
            }

            if (interactive)
            {
                WriteInfo("Bye!");
            }

            return resultStatus;
        }

        private static Dictionary<Type, IAction> GetActions()
        {
            var container = new Container();
            container.Collection.Register(typeof(IAction), Assembly.GetExecutingAssembly());
            container.RegisterSingleton<IDocumentSerializer, DocumentSerializer>();
            container.RegisterInstance(new DocumentTypeResolver().WithMessagingDocuments());

            return container
                .GetAllInstances<IAction>()
                .ToDictionary(t => t.OptionsType, t => t);
        }

        private static async Task ExecuteInteractiveModeAsync(Options options, IOnDemandClientChannel channel, Dictionary<Type, IAction> actionsDictionary)
        {
            while (channel.IsEstablished)
            {
                Console.Write("> ");
                var input = Console.ReadLine();

                if (string.IsNullOrEmpty(input) ||
                    input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                if (!channel.IsEstablished)
                {
                    WriteInfo("Channel is not established");
                    break;
                }

                await ExecuteActionAsync(input.Split(' '), channel, actionsDictionary, options.Timeout);
            }
        }

        private static async Task<bool> ExecuteActionAsync(string[] args, IOnDemandClientChannel channel, Dictionary<Type, IAction> actionsDictionary, int timeout)
        {
            var parsedCommandResult = Parser.ParseArguments(args, actionsDictionary.Values.Select(v => v.OptionsType).ToArray());
            if (parsedCommandResult.Tag == ParserResultType.NotParsed)
            {
                WriteInfo("Invalid action or invalid arguments");
                return false;
            }

            if (!actionsDictionary.TryGetValue(parsedCommandResult.TypeInfo.Current, out var action))
            {
                WriteInfo("Action type not found");
                return false;
            }

            var actionOptions = ((Parsed<object>)parsedCommandResult).Value;

            using (var cts = CreateCancellationTokenSource(timeout))
            {
                try
                {
                    await action.ExecuteAsync(actionOptions, channel, cts.Token);
                    return true;
                }
                catch (Exception ex)
                {
                    WriteError(ex.Message);
                    return false;
                }
            }
        }

        private static string[] GetActionArgs(string actionName, string[] args)
        {
            var actionArgsList = new List<string>() { actionName };
            var lastWasActionArg = false;

            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];

                if (arg.StartsWith("--action.", StringComparison.OrdinalIgnoreCase))
                {
                    actionArgsList.Add(arg.Replace("--action.", "--"));
                    lastWasActionArg = true;
                    continue;
                }

                if (lastWasActionArg && !arg.StartsWith("--"))
                {
                    actionArgsList.Add(arg);
                }

                lastWasActionArg = false;
            }

            return actionArgsList.ToArray();
        }

        private static CancellationTokenSource CreateCancellationTokenSource(int timeout)
        {
            return new CancellationTokenSource(TimeSpan.FromSeconds(timeout));
        }

        private static async Task<IOnDemandClientChannel> EstablishChannelAsync(ConnectionInformation connectionInformation, CancellationToken cancellationToken)
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
                WriteError("Could not establish the session: {0}", information.Exception.Message);
                return TaskUtil.FalseCompletedTask;
            });


            var channelListener = new ChannelListener(message =>
                {
                    WriteInfo("Message with id '{0}' received from '{1}': {2}", message.Id, message.GetSender(), message.Content);
                    return TaskUtil.TrueCompletedTask;
                },
                notification =>
                {
                    WriteInfo("Notification with id {0} received from '{1}' - Event: {2}", notification.Id, notification.GetSender(), notification.Event);
                    return TaskUtil.TrueCompletedTask;
                },
                command =>
                {
                    WriteInfo("Command with id '{0}' received from '{1}' - Method: {2} - URI: {3}", command.Id, command.GetSender(), command.Method, command.Uri);                    
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

        private static void WriteInfo(string format, params object[] args)
        {
            WriteLine(format, ConsoleColor.Gray, args);
        }

        private static void WriteWarning(string format, params object[] args)
        {
            WriteLine(format, ConsoleColor.Yellow, args);
        }

        private static void WriteError(string format, params object[] args)
        {
            WriteLine(format, ConsoleColor.Red, args);
        }

        private static void WriteLine(string format, ConsoleColor consoleColor, params object[] args)
        {
            Console.ForegroundColor = consoleColor;
            Console.WriteLine(format, args);
            Console.ResetColor();
        }
    }
}
