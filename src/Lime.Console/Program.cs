using Lime.Protocol;
using Lime.Protocol.Client;
using Lime.Protocol.Contents;
using Lime.Protocol.Network;
using Lime.Protocol.Resources;
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

            var notification = new Notification()
            {
                From = Node.Parse("andreb@takenet.com.br/casa"),
                To = Node.Parse("joao@takenet.com.br/trab"),
                Id = Guid.NewGuid(),
                Event = Event.Failed,
                Reason = new Reason()
                {
                    Code = 1,
                    Description = "Fudeu"
                }
            };

            var serialized = TypeSerializer.Serialize(notification);

            System.Console.WriteLine(serialized);
            System.Console.ReadLine();


            return;

            System.Console.Write("Port number (ENTER for default): ");

            var portNumberString = System.Console.ReadLine();
            int portNumber;

            if (!int.TryParse(portNumberString, out portNumber))
            {
                portNumber = 55321;
            }

            var uri = new Uri(string.Format("net.tcp://{0}:{1}", Dns.GetHostName(), portNumber));

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
                server.StopServerAsync().Wait();
                cancellationTokenSource.Cancel();

                System.Console.WriteLine("Server stoped. Press any key to exit.");
                System.Console.ReadLine();
            }
            else
            {

                var cancellationTokenSource = new CancellationTokenSource();

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


                System.Console.Write("New (N) or Existing (E) account (Enter for E): ");
                var createAccount = System.Console.ReadLine();

                bool accountExists = false;

                if (createAccount.ToUpper().Equals("N"))
                {
                    System.Console.WriteLine("Connecting in Guest mode...");

                    if (client.ConnectAsGuestAsync(identity.Domain, CancellationToken.None).Result)
                    {
                        System.Console.WriteLine("Connected. Creating the account...");

                        // Creates the account
                        var account = new Account()
                        {
                            Password = password.ToBase64()
                        };

                        var createAccountCommand = new Command()
                        {
                            From = new Node() 
                            { 
                                Name = identity.Name, 
                                Domain = identity.Domain 
                            },
                            Pp = client.Channel.LocalNode,
                            Method = CommandMethod.Set,
                            Resource = new Account()
                            {
                                Password = password.ToBase64()
                            }
                        };

                        client.Channel.SendCommandAsync(createAccountCommand).Wait();
                        var createAccountCommandResult = client.Channel.ReceiveCommandAsync(cancellationTokenSource.Token).Result;
                        
                        if (createAccountCommandResult.Status == CommandStatus.Success)
                        {
                            System.Console.WriteLine("Account created. Closing the session...");

                            client.Channel.SendFinishingSessionAsync().Wait();
                            client.Dispose();

                            client = new ConsoleClient(uri);
                            accountExists = true;                                                        
                        }
                        else
                        {
                            System.Console.WriteLine("Account creation failed: {0} - Reason: {1}", createAccountCommandResult.Status, createAccountCommandResult.Reason != null ? createAccountCommandResult.Reason.Description : "None");
                        }
                    }
                    else
                    {
                        System.Console.Write("Could not connect as a guest.");
                    }

                }
                else
                {
                    accountExists = true;
                }
                
                if (accountExists && 
                    client.ConnectAsync(identity, password, CancellationToken.None).Result)
                {
                    var listenMessagesTask = client.ReceiveMessagesAsync(cancellationTokenSource.Token);                   
                    listenMessagesTask
                        .ContinueWith(t =>
                        {
                            if (t.Exception != null)
                            {
                                System.Console.WriteLine("Exception: {0}", t.Exception);
                            }
                        });

                    var listenNotificationsTask = client.ReceiveNotificationsAsync(cancellationTokenSource.Token);
                    listenNotificationsTask
                        .ContinueWith(t =>
                        {
                            if (t.Exception != null)
                            {
                                System.Console.WriteLine("Exception: {0}", t.Exception);
                            }
                        });

                    System.Console.WriteLine("Setting the receipts...");
                    var receiptCommand = new Command()
                    {
                        Method = CommandMethod.Set,
                        Resource = new Receipt()
                        {
                            Events = new[] {  Event.Dispatched, Event.Received }
                        }
                    };
                    client.Channel.SendCommandAsync(receiptCommand).Wait();
                    var receiptCommandResult = client.Channel.ReceiveCommandAsync(cancellationTokenSource.Token).Result;
                    System.Console.WriteLine("Receipt result: {0} - Reason: {1}", receiptCommandResult.Status, receiptCommandResult.Reason != null ? receiptCommandResult.Reason.Description : "None");

                    System.Console.WriteLine("Getting account information...");
                    var accountCommand = new Command
                    {
                        Method = CommandMethod.Get,
                        Resource = new Account()
                    };
                    client.Channel.SendCommandAsync(accountCommand).Wait();
                    var accountCommandResult = client.Channel.ReceiveCommandAsync(cancellationTokenSource.Token).Result;
                    System.Console.WriteLine("Account result: {0} - Reason: {1}", accountCommandResult.Status, accountCommandResult.Reason != null ? accountCommandResult.Reason.Description : "None");



                    System.Console.WriteLine("Getting the roster...");
                    var rosterCommand = new Command
                    {
                        Method = CommandMethod.Get,
                        Resource = new Roster()
                    };
                    client.Channel.SendCommandAsync(rosterCommand).Wait();
                    var rosterCommandResult = client.Channel.ReceiveCommandAsync(cancellationTokenSource.Token).Result;

                    if (rosterCommandResult.Status == CommandStatus.Success)
                    {
                        System.Console.WriteLine("Contacts:");

                        var roster = rosterCommandResult.Resource as Roster;
                        foreach (var contact in roster.Contacts)
                        {
                            System.Console.WriteLine("- {0}", contact.Identity);
                        }
                    }
                    else
                    {
                        System.Console.WriteLine("Account result: {0} - Reason: {1}", rosterCommandResult.Status, rosterCommandResult.Reason != null ? rosterCommandResult.Reason.Description : "None");
                    }

                    if (accountCommandResult.Status == CommandStatus.Failure &&
                        accountCommandResult.Reason.Code == ReasonCodes.COMMAND_RESOURCE_NOT_FOUND)
                    {
                        System.Console.WriteLine("Setting account information...");

                        accountCommand = new Command
                        {
                            Method = CommandMethod.Set,
                            Resource = new Account()
                            {
                                Email = "myemail@bb.com",
                                City = "Albuquerque",
                                AllowAnonymousSender = false,
                                InboxSize = 100
                            }
                        };

                        client.Channel.SendCommandAsync(accountCommand).Wait();
                        accountCommandResult = client.Channel.ReceiveCommandAsync(cancellationTokenSource.Token).Result;
                        System.Console.WriteLine("Account result: {0} - Reason: {1}", accountCommandResult.Status, accountCommandResult.Reason != null ? accountCommandResult.Reason.Description : "None");
                    }

                    System.Console.WriteLine("Setting presence...");
                    var presenceCommand = new Command()
                    {
                        Method = CommandMethod.Set,
                        Resource = new Presence()
                        {
                            Status = PresenceStatus.Available,
                            Message = "I'm here!",
                            RoutingRule = RoutingRule.IdentityByDistance
                        }
                    };
                    client.Channel.SendCommandAsync(presenceCommand).Wait();
                    var presenceCommandResult = client.Channel.ReceiveCommandAsync(cancellationTokenSource.Token).Result;
                    System.Console.WriteLine("Presence result: {0} - Reason: {1}", presenceCommandResult.Status, presenceCommandResult.Reason != null ? presenceCommandResult.Reason.Description : "None");

                    System.Console.WriteLine("Client started. Type EXIT to quit.");

                    while (true)
                    {
                        System.Console.Write("Destination: ");

                        var toInput = System.Console.ReadLine();

                        if (toInput.Equals("exit", StringComparison.InvariantCultureIgnoreCase))
                        {
                            break;
                        }
                        else if (toInput.StartsWith("add"))
                        {
                            var commandIdentity = toInput.Split(' ');

                            Identity contactIdentity;

                            if (commandIdentity.Length > 1 &&
                                Identity.TryParse(commandIdentity[1], out contactIdentity))
                            {
                                System.Console.WriteLine("Adding contact...");

                                var contactCommand = new Command
                                {
                                    Method = CommandMethod.Set,
                                    Resource = new Roster()
                                    {
                                        Contacts = new Contact[]
                                        {
                                            new Contact()
                                            {
                                                Identity = contactIdentity
                                            }
                                        }                                        
                                    }
                                };

                                client.Channel.SendCommandAsync(contactCommand).Wait();
                                var contactCommandResult = client.Channel.ReceiveCommandAsync(cancellationTokenSource.Token).Result;
                                System.Console.WriteLine("Contact result: {0} - Reason: {1}", contactCommandResult.Status, contactCommandResult.Reason != null ? contactCommandResult.Reason.Description : "None");
                            }
                            else
                            {
                                System.Console.WriteLine("Invalid command. Try 'add name@domain.com'.");
                            }
                        }
                        else
                        {
                            Node to = null;
                            if (Node.TryParse(toInput, out to))
                            {
                                System.Console.Write("Message: ");

                                var messageText = System.Console.ReadLine();

                                var message = new Message()
                                {
                                    To = to,
                                    Content = new TextContent()
                                    {
                                        Text = messageText
                                    }
                                };

                                client.Channel.SendMessageAsync(message).Wait();
                            }
                        }
                    }                    
                                        
                    client.Disconnect(CancellationToken.None).Wait();
                    cancellationTokenSource.Cancel();
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