using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Lime.Protocol.Client;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Lime.Protocol.Serialization.Newtonsoft;
using Lime.Protocol.Server;
using Lime.Protocol.Util;
using Lime.Transport.Tcp;
using Lime.Transport.Tcp.UnitTests;
using static System.Console;

namespace Lime.Protocol.ConsoleTests
{
    class Program
    {
        private static Reporter _reporter = null;

        static void Main(string[] args) => MainAsync(args).GetAwaiter().GetResult();

        static async Task MainAsync(string[] args)
        {
            WriteLine("Starting the server...");
            
            var messageActionBlock = new ActionBlock<Message>(
                m => _reporter?.ReportEvent(),
                new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = DataflowBlockOptions.Unbounded,
                    MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded,
                    EnsureOrdered = false
                });
            
            var uri = new Uri("net.tcp://localhost:55321");

            RemoteCertificateValidationCallback certificateValidationCallback =
                (sender, certificate, chain, errors) => true;

            var server = new ServerBuilder(
                    "postmaster@msging.net/default",
                    new PipeTcpTransportListener(uri, CertificateUtil.CreateSelfSignedCertificate("localhost"),
                        new EnvelopeSerializer(new DocumentTypeResolver()),
                        clientCertificateValidationCallback: certificateValidationCallback))
                .WithChannelConsumers(m => messageActionBlock.SendAsync(m), n => TaskUtil.TrueCompletedTask,
                    c => TaskUtil.TrueCompletedTask)
                .WithEnabledEncryptionOptions(new SessionEncryption[] {SessionEncryption.None, SessionEncryption.TLS})
                .WithExceptionHandler(e =>
                {
                    ForegroundColor = ConsoleColor.Red;
                    WriteLine(e.ToString());
                    ResetColor();
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
                CursorLeft = cursorLeft;
                CursorTop = cursorTop;
                
                Write("Number of channels (ENTER for 1, EXIT to quit): ");
                
                var line = ReadLine();
                if (line != null && line.ToLowerInvariant().Equals("exit"))
                {
                    break;
                }

                if (!int.TryParse(line, out var channelCount))
                {
                    channelCount = 1;
                }
                
                Write("Envelope buffer size (ENTER for 1): ");
                if (!int.TryParse(ReadLine(), out var envelopeBufferSize))
                {
                    envelopeBufferSize = 1;
                }
                
                WriteLine("Starting the client...");

                var channelBuilder = ClientChannelBuilder
                    .Create(
                        () => new PipeTcpTransport(new EnvelopeSerializer(new DocumentTypeResolver()),
                            serverCertificateValidationCallback: certificateValidationCallback), uri)
                    .WithEnvelopeBufferSize(envelopeBufferSize)
                    .CreateEstablishedClientChannelBuilder()
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
                if (!int.TryParse(ReadLine(), out var taskCount))
                {
                    taskCount = 10;
                }

                Write("Number of messages (ENTER for 1000): ");
                if (!int.TryParse(ReadLine(), out var messagesCount))
                {
                    messagesCount = 1000;
                }

                _reporter = new Reporter(taskCount * messagesCount, CursorTop + 2);
                
                await Task.WhenAll(
                    Enumerable
                        .Range(0, taskCount)
                        .Select(i => Task.Run(async () =>
                        {
                            for (int j = 0; j < messagesCount; j++)
                            {
                                await client.SendMessageAsync(
                                    new Message()
                                    {
                                        Id = $"{i}_{j}",
                                        Content = "Testing a message"
                                    },
                                    CancellationToken.None);
                            }
                        })));

                _reporter.ReportSendComplete();
                await _reporter.ReportTask;
                _reporter = null;

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
            
            await server.StopAsync(CancellationToken.None);

            WriteLine("Server stopped. Press ENTER to exit.");
            ReadLine();
        }
    }

    public class Reporter
    {
        private readonly int _expectedCount;
        private readonly int _cursorTop;
        private readonly DateTime _startDateTime;
        private readonly Stopwatch _stopwatch;
        private int _counter;
        private static double _maxAverage;
        private TimeSpan _sendComplete;
        
        public Reporter(int expectedCount, int cursorTop)
        {
            _expectedCount = expectedCount;
            _cursorTop = cursorTop;
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
                WriteLine("Sending in progress...");
            }
        }
    }
}