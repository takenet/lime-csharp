using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Lime.Protocol.Client;
using Lime.Protocol.Network;
using Lime.Protocol.Network.Modules;
using Lime.Protocol.Serialization;
using Lime.Protocol.Serialization.Newtonsoft;
using Lime.Protocol.Server;
using Lime.Protocol.Util;
using Lime.Transport.Tcp;
using Lime.Transport.Tcp.UnitTests;
using Lime.Transport.WebSocket;
using static System.Console;

namespace Lime.Protocol.ConsoleTests
{
    class Program
    {
        private static Reporter _reporter = null;

        static void Main(string[] args) => MainAsync(args).GetAwaiter().GetResult();

        static async Task MainAsync(string[] args)
        {
            Clear();
            WriteLine("Available transport types:");
            WriteLine("1 - TcpTransport (default)");
            WriteLine("2 - PipeTcpTransport");
            WriteLine("3 - WebSocketTransport");
            WriteLine("4 - PipeWebSocketTransport");
            
            Write("Select type: ");
            if (!int.TryParse(ReadLine(), out var transportType))
            {
                transportType = 1;
            }
            
            Func<ITransportListener> transportListenerFactory;
            Func<ITransport> clientTransportFactory;
            Uri uri;
            var envelopeSerializer = new EnvelopeSerializer(new DocumentTypeResolver());
            RemoteCertificateValidationCallback certificateValidationCallback = (sender, certificate, chain, errors) => true;
            
            switch (transportType)
            {
                case 2:
                    uri = new Uri("net.tcp://localhost:55322");
                    transportListenerFactory = () =>
                        new PipeTcpTransportListener(
                            uri, 
                            CertificateUtil.CreateSelfSignedCertificate("localhost"),
                            new EnvelopeSerializer(new DocumentTypeResolver()),
                            clientCertificateValidationCallback: certificateValidationCallback);
                    clientTransportFactory = () => new PipeTcpTransport(envelopeSerializer, serverCertificateValidationCallback: certificateValidationCallback);
                    break;
                
                case 3:
                    uri = new Uri("wss://localhost:8081");
                    transportListenerFactory = () =>
                        new WebSocketTransportListener(
                            new[] { uri }, 
                            envelopeSerializer,
                            CertificateUtil.CreateSelfSignedCertificate("localhost"),
                            clientCertificateValidationCallback: (clientCertificate, chain, sslPolicyErrors) => certificateValidationCallback(null, clientCertificate, chain, sslPolicyErrors),
                            closeGracefully: false);
                    clientTransportFactory = () => new ClientWebSocketTransport(envelopeSerializer, serverCertificateValidationCallback: certificateValidationCallback);                    
                    break;
                
                case 4:
                    uri = new Uri("wss://localhost:8082");
                    transportListenerFactory = () =>
                        new PipeWebSocketTransportListener(
                            new[] { uri }, 
                            envelopeSerializer,
                            CertificateUtil.CreateSelfSignedCertificate("localhost"),
                            clientCertificateValidationCallback: (clientCertificate, chain, sslPolicyErrors) => certificateValidationCallback(null, clientCertificate, chain, sslPolicyErrors),
                            closeGracefully: false);
                    clientTransportFactory = () => new PipeClientWebSocketTransport(envelopeSerializer, serverCertificateValidationCallback: certificateValidationCallback);                         
                    break;
                
                default:
                    uri = new Uri("net.tcp://localhost:55321");
                    transportListenerFactory = () =>
                        new TcpTransportListener(
                            uri, 
                            CertificateUtil.CreateSelfSignedCertificate("localhost"),
                            envelopeSerializer,
                            clientCertificateValidationCallback: certificateValidationCallback);
                    clientTransportFactory = () => new TcpTransport(envelopeSerializer, serverCertificateValidationCallback: certificateValidationCallback);
                    break;
            }

            WriteLine("Starting the server...");

            var actionBlockOptions = new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = DataflowBlockOptions.Unbounded,
                MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded,
                EnsureOrdered = false
            };
            
            var reportActionBlock = new ActionBlock<Envelope>(
                m => _reporter?.ReportEvent(),
                actionBlockOptions);

            
            var server = new ServerBuilder("postmaster@msging.net/default", transportListenerFactory())
                .WithChannelConsumers(
                    m => reportActionBlock.SendAsync(m), 
                    n => reportActionBlock.SendAsync(n),
                    c => reportActionBlock.SendAsync(c))
                .WithEnabledEncryptionOptions(new SessionEncryption[] {SessionEncryption.None, SessionEncryption.TLS})
                .WithExceptionHandler(e =>
                {
                    var cursorTop = CursorTop;
                    CursorTop = 20;
                    ForegroundColor = ConsoleColor.Red;
                    WriteLine(e.ToString());
                    ResetColor();
                    CursorTop = cursorTop;
                    return Task.CompletedTask;
                })
                .WithEnvelopeBufferSize(-1)
                .Build();
            
            await server.StartAsync(CancellationToken.None);

            WriteLine("Server started.");

            var cursorLeft = CursorLeft;
            var cursorTop = CursorTop;

            while (true)
            {
                CursorLeft = cursorLeft;
                CursorTop = cursorTop;
                WriteLine("                                                            ");
                WriteLine("                                                            ");
                WriteLine("                                                            ");
                WriteLine("                                                            ");
                WriteLine("                                                            ");
                WriteLine("                                                            ");
                WriteLine("                                                            ");
                WriteLine("                                                            ");
                WriteLine("                                                            ");
                WriteLine("                                                            ");
                CursorLeft = cursorLeft;
                CursorTop = cursorTop;
                
                Write("Number of channels (ENTER for 1, EXIT to quit): ");
                
                var line = ReadLine();
                if (line != null && line.ToLowerInvariant().Equals("exit"))
                {
                    break;
                }

                if (!uint.TryParse(line, out var channelCount))
                {
                    channelCount = 1;
                }
                
                Write("Envelope buffer size (ENTER for 1): ");
                if (!int.TryParse(ReadLine(), out var envelopeBufferSize))
                {
                    envelopeBufferSize = 1;
                }
                
                Write("Send batch size (ENTER for 1): ");
                if (!uint.TryParse(ReadLine(), out var sendBatchSize))
                {
                    sendBatchSize = 1;
                }
                
                Write("Module delay (ENTER for 0): ");
                if (!uint.TryParse(ReadLine(), out var moduleDelay))
                {
                    moduleDelay = 0;
                }
                
                WriteLine("Starting the client...");

                var delayMessageChannelModule = new ChannelModule<Message>(
                    async (message, token) =>
                    {
                        await Task.Delay((int)moduleDelay, token);
                        return message;
                    },
                    async (message, token) =>
                    {
                        await Task.Delay((int)moduleDelay, token);
                        return message;
                    },
                    state => { });
                
                var channelBuilder = ClientChannelBuilder
                    .Create(clientTransportFactory, uri)
                    .WithEnvelopeBufferSize(envelopeBufferSize)
                    .WithSendBatchSize((int)sendBatchSize)
                    .WithSendFlushBatchInterval(TimeSpan.FromMilliseconds(1000))
                    .CreateEstablishedClientChannelBuilder()
                    .AddEstablishedHandler((channel, token) =>
                    {
                        if (moduleDelay > 0)
                        {
                            channel.MessageModules.Add(delayMessageChannelModule);
                        }
                        return Task.CompletedTask;
                    })
                    .WithEncryption(SessionEncryption.TLS);

                IEstablishedChannel client;

                if (channelCount > 1)
                {
                    client = new MultiplexerClientChannel(channelBuilder);
                    await ((MultiplexerClientChannel)client).EstablishAsync(CancellationToken.None);
                }
                else
                {
                    client = await channelBuilder.BuildAndEstablishAsync(CancellationToken.None);                    
                }
                
                WriteLine("Client started."); 
                
                Write("Number of tasks (ENTER for 10): ");
                if (!uint.TryParse(ReadLine(), out var taskCount))
                {
                    taskCount = 10;
                }

                Write("Number of messages (ENTER for 1000): ");
                if (!uint.TryParse(ReadLine(), out var messagesCount))
                {
                    messagesCount = 1000;
                }
                
                Write("Number of notifications (ENTER for 1000): ");
                if (!uint.TryParse(ReadLine(), out var notificationsCount))
                {
                    notificationsCount = 1000;
                }
                
                Write("Number of commands (ENTER for 1000): ");
                if (!uint.TryParse(ReadLine(), out var commandsCount))
                {
                    commandsCount = 1000;
                }

                _reporter = new Reporter(
                    (int)(taskCount * (messagesCount + notificationsCount + commandsCount)), 
                    CursorTop + 2, 
                    $"Transp {transportType} Ch {channelCount} Buf {envelopeBufferSize} Bat {sendBatchSize} Delay {moduleDelay} Tasks {taskCount} Msgs {messagesCount} Not {notificationsCount} Cmds {commandsCount}");
                
                var to = Node.Parse("name@domain/instance");
                var limeUri = new LimeUri("/ping");
                
                await Task.WhenAll(
                    Enumerable
                        .Range(0, (int)taskCount)
                        .Select(i => Task.Run(async () =>
                        {
                            var messagesTask = Task.Run(async () =>
                            {
                                for (int j = 0; j < messagesCount; j++)
                                {
                                    await client.SendMessageAsync(
                                        new Message()
                                        {
                                            Id = $"{i}_{j}",
                                            To = to,
                                            Content = "Testing a message"
                                        },
                                        CancellationToken.None);
                                }
                            });
                            
                            var notificationsTask = Task.Run(async () =>
                            {
                                for (int j = 0; j < notificationsCount; j++)
                                {
                                    await client.SendNotificationAsync(
                                        new Notification()
                                        {
                                            Id = $"{i}_{j}",
                                            To = to,
                                            Event = Event.Received
                                        },
                                        CancellationToken.None);
                                }
                            });
                            
                            var commandsTask = Task.Run(async () =>
                            {
                                for (int j = 0; j < commandsCount; j++)
                                {
                                    await client.SendCommandAsync(
                                        new Command()
                                        {
                                            Id = $"{i}_{j}",
                                            To = to,
                                            Method= CommandMethod.Observe,
                                            Uri = limeUri
                                        },
                                        CancellationToken.None);
                                }
                            });

                            await Task.WhenAll(messagesTask, notificationsTask, commandsTask);
                        })));

                _reporter.ReportSendComplete();
                await _reporter.ReportTask;
                _reporter = null;

                try
                {
                    if (client is IOnDemandClientChannel onDemandClientChannel)
                    {
                        await onDemandClientChannel.FinishAsync(default);
                    }
                    else if (client is IClientChannel clientChannel)
                    {
                        await clientChannel.SendFinishingSessionAsync(default);
                        await clientChannel.ReceiveFinishedSessionAsync(default);
                    }

                    client.DisposeIfDisposable();
                }
                catch
                {
                }
            }
            
            await server.StopAsync(CancellationToken.None);

            WriteLine("Server stopped. Press ENTER to exit.");
            ReadLine();
        }
    }

    public class Reporter
    {
        private readonly int _expectedCount;
        private readonly int _cursorTop;
        private readonly string _setupDescription;
        private readonly DateTime _startDateTime;
        private readonly Stopwatch _stopwatch;
        private int _counter;
        private double _maxAverage;
        private TimeSpan _sendComplete;
        
        public Reporter(int expectedCount, int cursorTop, string setupDescription)
        {
            _expectedCount = expectedCount;
            _cursorTop = cursorTop;
            _setupDescription = setupDescription;
            _startDateTime = DateTime.UtcNow;
            _stopwatch = Stopwatch.StartNew();
            _counter = 0;
            ClearScreen();
            ReportTask = Task.Run(DoReport);
        }

        public Task ReportTask { get; }

        public TimeSpan Elapsed => _stopwatch.Elapsed;

        public void ReportEvent()
        {
            if (Interlocked.Increment(ref _counter) >= _expectedCount)
            {
                _stopwatch.Stop();
            }
        }

        public void ReportSendComplete()
        {
            _sendComplete = _stopwatch.Elapsed;
        }

        private void ClearScreen()
        {
            SetCursorPosition(0, _cursorTop);
            Write("                                                 ");
            Write("                                                 ");
            Write("                                                 ");
            Write("                                                 ");
            Write("                                                 ");
            Write("                                                 ");
            Write("                                                 ");
        }

        private async Task DoReport()
        {
            while (_counter < _expectedCount)
            {
                await Task.Delay(1000);
                Report();
            }
        }

        private void Report()
        {
            SetCursorPosition(0, _cursorTop);
            WriteLine($"Setup: {_setupDescription}");
            WriteLine($"Count: {_counter}                               ");
            WriteLine($"Start time: {_startDateTime}                    ");
            WriteLine($"Elapsed: {Elapsed.ToString("g", CultureInfo.InvariantCulture)}                    ");
            double average = 0;
            if (Elapsed.TotalSeconds > 0)
            {
                average = _counter / Elapsed.TotalSeconds;
            }

            WriteLine($"Average: {average:N3} messages/second           ");
            if (average > _maxAverage)
            {
                _maxAverage = average;
            }

            WriteLine($"Max average: {_maxAverage:N3} messages/second           ");

            if (_sendComplete != default)
            {
                WriteLine($"Sending complete at: {_sendComplete.ToString("g", CultureInfo.InvariantCulture)}                    ");
            }
            else
            {
                WriteLine("Sending in progress...                                ");
            }
        }
    }
}