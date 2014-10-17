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

namespace Lime.Client.Console
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

            var hostName = System.Console.ReadLine();
            if (string.IsNullOrWhiteSpace(hostName))
            {
                hostName = Dns.GetHostName();
            }

            System.Console.Write("Port number (ENTER for default): ");

            var portNumberString = System.Console.ReadLine();
            int portNumber;

            if (!int.TryParse(portNumberString, out portNumber))
            {
                portNumber = 55321;
            }

            var uri = new Uri(string.Format("net.tcp://{0}:{1}", hostName, portNumber));

            string option = null;

            while (option != "s" && option != "c" && option != "S" && option != "C")
            {
                System.Console.Write("Choose a working mode - Server (s) or Client (c): ");
                option = System.Console.ReadLine();
            }

            if (option.ToLower() == "s")
            {
                await StartAsServerAsync(uri);
            }
            else
            {
                await StartAsClientAsync(uri);
            }
        }

        private static async Task StartAsServerAsync(Uri uri)
        {
            var server = new ConsoleServer(uri);
            await server.StartServerAsync();

            var cancellationTokenSource = new CancellationTokenSource();
            var listenTask = server.ListenAsync(cancellationTokenSource.Token);

            System.Console.WriteLine("Server started. Press any key to stop.");
            System.Console.ReadLine();
            await server.StopServerAsync();
            cancellationTokenSource.Cancel();

            System.Console.WriteLine("Server stopped. Press any key to exit.");
            System.Console.ReadLine();
        }

        private static async Task StartAsClientAsync(Uri uri)
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

            System.Console.Write("Use (P)assword or (C)ertificate? ");
            var authenticationScheme = System.Console.ReadLine();

            X509Certificate2 clientCertificate = null;
            string password = null;
            bool accountExists = false;

            if (authenticationScheme.Equals("c", StringComparison.OrdinalIgnoreCase))
            {
                // Finds the certificate for the identity
                var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadOnly);

                var certificates = store.Certificates.Find(X509FindType.FindBySubjectName, identity.ToString(), true);

                if (certificates.Count > 0)
                {
                    clientCertificate = certificates[0];
                    accountExists = true;
                }
                else
                {
                    System.Console.Write(string.Format("Client certificate not found for '{0}'", identity.ToString()));
                    password = System.Console.ReadLine();
                }
            }
            else
            {
                System.Console.Write("Enter the password: ");
                password = System.Console.ReadLine();

                System.Console.Write("New (N) or Existing (E) account (Enter for E): ");
                var createAccount = System.Console.ReadLine();

                if (createAccount.ToUpper().Equals("N"))
                {
                    System.Console.WriteLine("Connecting in Guest mode...");

                    if (await client.ConnectAsGuestAsync(identity.Domain, CancellationToken.None))
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
                        
                        var createAccountCommandResult = await client.Channel.ProcessCommandAsync(createAccountCommand, cancellationTokenSource.Token);

                        if (createAccountCommandResult.Status == CommandStatus.Success)
                        {
                            System.Console.WriteLine("Account created. Closing the session...");

                            await client.Channel.SendFinishingSessionAsync();
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
            }


            System.Console.WriteLine("Connecting...");

            if (accountExists &&
                ((password != null && await client.ConnectWithPasswordAsync(identity, password, cancellationTokenSource.Token)) ||
                (clientCertificate != null && await client.ConnectWithCertificateAsync(identity, clientCertificate, cancellationTokenSource.Token))))
            {
                System.Console.WriteLine("Connected.");

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

                var receiptCommandResult = await client.SetReceiptsAsync(cancellationTokenSource.Token);

                System.Console.WriteLine("Receipt result: {0} - Reason: {1}", receiptCommandResult.Status, receiptCommandResult.Reason != null ? receiptCommandResult.Reason.Description : "None");

                System.Console.WriteLine("Getting account information...");

                var accountCommandResult = await client.GetAccountAsync(cancellationTokenSource.Token);

                System.Console.WriteLine("Account result: {0} - Reason: {1}", accountCommandResult.Status, accountCommandResult.Reason != null ? accountCommandResult.Reason.Description : "None");

                System.Console.WriteLine("Getting the roster...");

                var rosterCommandResult = await client.GetContactsAsync(cancellationTokenSource.Token);

                if (rosterCommandResult.Status == CommandStatus.Success)
                {
                    System.Console.WriteLine("Contacts:");

                    var roster = rosterCommandResult.Resource as DocumentCollection;
                    foreach (Contact contact in roster.Items)
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

                    var account = new Account()
                    {
                        Email = "myemail@bb.com",
                        City = "Albuquerque",
                        AllowAnonymousSender = false,
                        InboxSize = 100
                    };

                    accountCommandResult = await client.SetAccountAsync(account, cancellationTokenSource.Token);
                    System.Console.WriteLine("Account result: {0} - Reason: {1}", accountCommandResult.Status, accountCommandResult.Reason != null ? accountCommandResult.Reason.Description : "None");
                }

                System.Console.WriteLine("Setting presence...");

                var presence = new Presence()
                {
                    Status = PresenceStatus.Available,
                    Message = "I'm here!",
                    RoutingRule = RoutingRule.IdentityByDistance
                };

                var presenceCommandResult = await client.SetPresenceAsync(presence, cancellationTokenSource.Token);
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

                            var contact = new Contact()
                            {
                                Identity = contactIdentity
                            };

                            var contactCommandResult = await client.SetContactsAsync(contact, cancellationTokenSource.Token);

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
                            await client.SendTextMessageAsync(to, messageText);
                        }
                    }
                }

                await client.DisconnectAsync(CancellationToken.None);
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