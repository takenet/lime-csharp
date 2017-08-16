using System;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Serialization.Newtonsoft;
using Lime.Protocol.Server;
using Lime.Transport.Tcp;
using static System.Console;

namespace Lime.Protocol.ConsoleTests
{
    class Program
    {
        static void Main(string[] args) => MainAsync(args).GetAwaiter().GetResult();

        static async Task MainAsync(string[] args)
        {
            var server = new ServerBuilder(
                    "postmaster@msging.net/default",
                    new TcpTransportListener(new Uri("net.tcp://localhost:55321"), null, new JsonNetSerializer()))
                .WithExceptionHandler(e =>
                {
                    ForegroundColor = ConsoleColor.Red;
                    WriteLine(e.ToString());
                    ResetColor();
                    return Task.CompletedTask;                    
                })
                .Build();


            await server.StartAsync(CancellationToken.None);

            
            WriteLine("Server started. Press ENTER to stop.");
            ReadLine();

            await server.StopAsync(CancellationToken.None);

            WriteLine("Server stopped. Press ENTER to exit.");
            ReadLine();

        }
    }
}
