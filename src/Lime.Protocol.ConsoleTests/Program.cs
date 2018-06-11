using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Lime.Protocol.Client;
using Lime.Protocol.Serialization.Newtonsoft;
using Lime.Protocol.Server;
using Lime.Protocol.Util;
using Lime.Transport.Tcp;
using static System.Console;

namespace Lime.Protocol.ConsoleTests
{
    class Program
    {
        private static DateTime _startDateTime;
        private static int _counter;
        private static double _maxAverage;

        static void Main(string[] args) => MainAsync(args).GetAwaiter().GetResult();

        static async Task MainAsync(string[] args)
        {
            WriteLine("Starting the server...");

            var messageBufferBlock = new BufferBlock<Message>(
                new DataflowBlockOptions
                {
                    BoundedCapacity = DataflowBlockOptions.Unbounded
                }
            );
            var messageActionBlock = new ActionBlock<Message>(
                ReceiveMessageAsync,
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded,
                    EnsureOrdered = false
                });

            messageBufferBlock.LinkTo(messageActionBlock);

            var uri = new Uri("net.tcp://localhost:55321");

            var server = new ServerBuilder(
                    "postmaster@msging.net/default",
                    new TcpTransportListener(uri, null, new JsonNetSerializer()))
                .WithChannelConsumers(m => messageBufferBlock.SendAsync(m), n => TaskUtil.TrueCompletedTask, c => TaskUtil.TrueCompletedTask)
                .WithEnabledEncryptionOptions(new SessionEncryption[] { SessionEncryption.TLS })
                .WithExceptionHandler(e =>
                {
                    ForegroundColor = ConsoleColor.Red;
                    WriteLine(e.ToString());
                    ResetColor();
                    return Task.CompletedTask;
                })
                .Build();



            await server.StartAsync(CancellationToken.None);

            using (var cts = new CancellationTokenSource())
            {

                WriteLine("Server started.");
                WriteLine("Starting the client...");


                

                var channelBuilder =  ClientChannelBuilder
                    .Create(() => new TcpTransport(new JsonNetSerializer()), uri)
                    .CreateEstablishedClientChannelBuilder()
                    .WithEncryption(SessionEncryption.TLS);
                
                
                var client = new MultiplexerClientChannel(channelBuilder);
                await client.EstablishAsync(CancellationToken.None);

                //var client = await channelBuilder.BuildAndEstablishAsync(CancellationToken.None);

                WriteLine("Client started.");

                var reportTask = Task.Run(() => DoReport(cts.Token), cts.Token);

                while (true)
                {
                    SetCursorPosition(0, 5);
                    Write("                                                 ");
                    Write("                                                 ");
                    Write("                                                 ");
                    Write("                                                 ");
                    Write("                                                 ");
                    SetCursorPosition(0, 5);


                    Write("Number of tasks (ENTER for default): ");
                    if (!int.TryParse(Console.ReadLine(), out var taskCount))
                    {
                        taskCount = 10;
                    }

                    Write("Number of messages (ENTER for default): ");
                    if (!int.TryParse(Console.ReadLine(), out var messagesCount))
                    {
                        messagesCount = 1000;
                    }

                    Reset();

                    var stopwatch = Stopwatch.StartNew();

                    await Task.WhenAll(
                        Enumerable
                        .Range(0, taskCount)
                        .Select(i => Task.Run(async () =>
                        {
                            for (int j = 0; j < messagesCount; j++)
                            {
                                await client.SendMessageAsync(new Message()
                                {
                                    Id = $"{i}_{j}",
                                    Content = "Testing a message"
                                },
                                CancellationToken.None);
                            }

                        })));

                    stopwatch.Stop();

                    WriteLine($"Elapsed: {stopwatch.ElapsedMilliseconds} ms             ");

                }

                cts.Cancel();
                await reportTask;
            }

            await server.StopAsync(CancellationToken.None);

            WriteLine("Server stopped. Press ENTER to exit.");
            ReadLine();
        }

        private static Task ReceiveMessageAsync(Message message)
        {
            Interlocked.Increment(ref _counter);
            return Task.CompletedTask;
        }

        private static async Task DoReport(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000, cancellationToken);
                Report();
            }
        }

        private static void Reset()
        {
            _startDateTime = DateTime.UtcNow;
            _counter = 0;
            _maxAverage = 0;
        }

        private static void Report()
        {
            var cursorLeft = CursorLeft;
            var cursorTop = CursorTop;

            var elapsed = DateTime.UtcNow - _startDateTime;
            SetCursorPosition(0, 10);
            WriteLine($"Count: {_counter}                               ");
            WriteLine($"Start time: {_startDateTime}                    ");
            WriteLine($"Elapsed: {elapsed.ToString("g", CultureInfo.InvariantCulture)}                    ");
            double average = 0;
            if (elapsed.TotalSeconds > 0)
            {
                average = _counter / elapsed.TotalSeconds;
            }
            WriteLine($"Average: {average:N3} messages/second           ");
            if (average > _maxAverage)
            {
                _maxAverage = average;
            }
            WriteLine($"Max average: {_maxAverage:N3} messages/second           ");

            SetCursorPosition(cursorLeft, cursorTop);
        }

    }
}
