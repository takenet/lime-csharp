using Lime.Protocol;
using Lime.Protocol.Client;
using Lime.Protocol.Contents;
using Lime.Protocol.Network;
using Lime.Protocol.Security;
using Lime.Protocol.Serialization;
using Lime.Protocol.Server;
using Lime.Protocol.Tcp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Console
{
    class Program
    {
        static void Main(string[] args)
        {           
            var uri = new Uri(string.Format("net.tcp://{0}:55321", Dns.GetHostName()));

            string option = null;

            while (option != "s" && option != "c" && option != "S" && option != "C")
            {                
                System.Console.Write("Choose a working mode - Server (s) or Client (c): ");
                option = System.Console.ReadLine();
            }

            if (option.ToLower() == "s")
            {
                var server = new ConsoleServer(uri);
                server.StartServerAsync().Wait();
                
                var cancellationTokenSource = new CancellationTokenSource();
                var listenTask = server.ListenAsync(cancellationTokenSource.Token);

                System.Console.WriteLine("Server started. Press any key to stop.");
                System.Console.ReadLine();
                cancellationTokenSource.Cancel();

                server.StopServerAsync().Wait();

                System.Console.WriteLine("Server stoped. Press any key to exit.");
                System.Console.ReadLine();
            }
            else
            {
                var client = new ConsoleClient(uri);

                string identityInput = null;
                Identity identity;

                while (!Identity.TryParse(identityInput, out identity))
	            {
	                System.Console.Write("Enter the identity (name@domain): ");
                    identityInput = System.Console.ReadLine();
	            }
                
                System.Console.Write("Enter the password: ");
                var password = System.Console.ReadLine();

                if (client.ConnectAsync(identity, password, CancellationToken.None).Result)
                {
                    var cancellationTokenSource = new CancellationTokenSource();
                    var listenTask = client.ReceiveMessagesAsync(cancellationTokenSource.Token);

                    System.Console.Write("Client started. Press any key to stop.");
                    System.Console.ReadLine();
                    cancellationTokenSource.Cancel();
                    client.Disconnect(CancellationToken.None).Wait();
                }
                else
                {
                    System.Console.Write("Could not connect. ");
                }
                System.Console.Write("Press any key to exit.");
                System.Console.ReadLine();
                                
            }

        }       
    }
}
