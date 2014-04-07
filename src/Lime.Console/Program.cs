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
        private static IDictionary<Guid, IServerChannel> _serverConnectedNodesDictionary;
        private static IDictionary<Node, Guid> _serverNodeSessionIdDictionary;
        private static Uri _listenerUri;

        private static IDictionary<Identity, string> _identityPasswordDictionary;

        static void Main(string[] args)
        {
            //TestSerializers();
            //return;

            _identityPasswordDictionary = new Dictionary<Identity, string>
            {
                { Identity.Parse("john@domain.com") , "123456" },
                { Identity.Parse("paul@domain.com") , "abcdef" }

            };

            _serverConnectedNodesDictionary = new Dictionary<Guid, IServerChannel>();
            _serverNodeSessionIdDictionary = new Dictionary<Node, Guid>();

            _listenerUri = new Uri("net.tcp://notebires.takenet.com.br:55321");

            var listener = StartServerAsync().Result;

            var clientChannel = StartClientAsync().Result;

            System.Console.ReadLine();

            clientChannel.SendFinishingSessionAsync().Wait();
        }

        private static void TestSerializers()
        {
            var authenticatingJson = "{\"state\":\"authenticating\",\"scheme\":\"plain\",\"authentication\":{\"password\":\"bXlwYXNzd29yZA==\"},\"id\":\"458f5c19-5655-47c9-8f67-a064c5f9f9d6\",\"from\":\"andreb@takenet.com.br/NOTEBIRES\"}";

            var json = authenticatingJson;

            var serializer1 = new EnvelopeSerializer();
            var serializer2 = new Lime.Protocol.Serialization.Newtonsoft.JsonNetSerializer();
            var serializer3 = new Lime.Protocol.Serialization.ServiceStack.ServiceStackSerializer();

            Envelope envelope1 = null, envelope2 = null, envelope3 = null;

            int count = 100000;

            System.Console.WriteLine("Deserialization:");


            var sw1 = System.Diagnostics.Stopwatch.StartNew();

            for (int i = 0; i < count; i++)
            {
                envelope1 = serializer1.Deserialize(json);
            }
            sw1.Stop();

            System.Console.WriteLine("EnvelopeSerializer: {0} ms", sw1.ElapsedMilliseconds);


            var sw2 = System.Diagnostics.Stopwatch.StartNew();

            for (int i = 0; i < count; i++)
            {
                envelope2 = serializer2.Deserialize(json);
            }
            sw2.Stop();

            System.Console.WriteLine("JsonNetSerializer: {0} ms", sw2.ElapsedMilliseconds);


            var sw3 = System.Diagnostics.Stopwatch.StartNew();

            for (int i = 0; i < count; i++)
            {
                envelope3 = serializer3.Deserialize(json);
            }
            sw3.Stop();

            System.Console.WriteLine("ServiceStackSerializer: {0} ms", sw3.ElapsedMilliseconds);

            var json1 = serializer2.Serialize(envelope1);
            var json2 = serializer2.Serialize(envelope1);
            var json3 = serializer2.Serialize(envelope1);

            if (json1 == json2 && json2 == json3)
            {
                System.Console.WriteLine("All deserialized types are equals");
            }
            else
            {
                System.Console.WriteLine("Deserialized types NOT equals: ");
            }

            var envelope = envelope1;

            System.Console.WriteLine("Serialization:");


            sw1 = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
            {
                json1 = serializer1.Serialize(envelope);
            }
            sw1.Stop();

            System.Console.WriteLine("EnvelopeSerializer: {0} ms", sw1.ElapsedMilliseconds);


            sw2 = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
            {
                json2 = serializer2.Serialize(envelope);
            }
            sw2.Stop();

            System.Console.WriteLine("JsonNetSerializer: {0} ms", sw2.ElapsedMilliseconds);


            sw3 = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
            {
                json3 = serializer3.Serialize(envelope);
            }
            sw3.Stop();

            System.Console.WriteLine("ServiceStackSerializer: {0} ms", sw3.ElapsedMilliseconds);

            if (json1 == json2 && json2 == json3)
            {
                System.Console.WriteLine("All serialized types are equals");
            }
            else
            {
                System.Console.WriteLine("Serialized types NOT equals: ");
            }

            System.Console.Read();
        }

        #region Start Methods

        private static async Task<ITransportListener> StartServerAsync()
        {
            var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);

            var certificates = store.Certificates.Find(X509FindType.FindByThumbprint, "10f422b0d59269ac13cb9ba73ba18f8ccbe58694", false);

            if (certificates.Count == 0)
            {
                throw new InvalidOperationException("Server certificate not found");
            }          

            ITransportListener listener = new TcpTransportListener(
                certificates[0],
                new EnvelopeSerializer(),
                new DebugTraceWriter("Server")
                );

            listener.Connected += Listener_Connected;

            await listener.StartAsync(_listenerUri);

            return listener;
        }

        private async static Task<IClientChannel> StartClientAsync()
        {
            var tcpClient = new TcpClient();

            ITransport transport = new TcpTransport(
                new TcpClientAdapter(tcpClient),
                new EnvelopeSerializer(),
                traceWriter: new DebugTraceWriter("Client"),
                hostName: _listenerUri.Host
                );

            var clientChannel = new ClientChannel(transport, TimeSpan.FromSeconds(60));
            clientChannel.NegotiateSessionReceived += ClientChannel_NegotiateSessionReceived;
            clientChannel.AuthenticateSessionReceived += ClientChannel_AuthenticateSessionReceived;
            clientChannel.SessionEstablished += ClientChannel_SessionEstablished;
            clientChannel.SessionFinished += ClientChannel_SessionFinished;
            clientChannel.SessionFailed += ClientChannel_SessionFailed;
            clientChannel.MessageReceived += ClientChannel_MessageReceived;
            clientChannel.CommandReceived += ClientChannel_CommandReceived;
            clientChannel.NotificationReceived += ClientChannel_NotificationReceived;

            await clientChannel.Transport.OpenAsync(_listenerUri, CancellationToken.None);

            await clientChannel.SendNewSessionAsync();

            return clientChannel;
        }

        #endregion

        #region Client Event Handlers


        private async static void ClientChannel_NegotiateSessionReceived(object sender, EnvelopeEventArgs<Session> e)
        {
            IClientChannel channel = (IClientChannel)sender;

            if (e.Envelope.CompressionOptions != null ||
                e.Envelope.EncryptionOptions != null)
            {
                await channel.SendNegotiatingSessionAsync(e.Envelope.CompressionOptions.First(), e.Envelope.EncryptionOptions.Last());
            }
            else if (e.Envelope.Encryption.HasValue && e.Envelope.Compression.HasValue)
            {
                if (channel.Transport.Compression != e.Envelope.Compression.Value)
                {
                    await channel.Transport.SetCompressionAsync(e.Envelope.Compression.Value, CancellationToken.None);                    
                }

                if (channel.Transport.Encryption != e.Envelope.Encryption.Value)
                {
                    await channel.Transport.SetEncryptionAsync(e.Envelope.Encryption.Value, CancellationToken.None);
                }
            }
        }


        private async static void ClientChannel_AuthenticateSessionReceived(object sender, EnvelopeEventArgs<Session> e)
        {
            IClientChannel channel = (IClientChannel)sender;

            await channel.SendAuthenticatingSessionAsync(
                new Identity() { Name = Environment.UserName, Domain = IPGlobalProperties.GetIPGlobalProperties().DomainName },
                new PlainAuthentication() { Password = Convert.ToBase64String(Encoding.UTF8.GetBytes("mypassword")) },
                instance: Environment.MachineName,
                sessionMode: SessionMode.Node);
        }


        private async static void ClientChannel_SessionEstablished(object sender, EnvelopeEventArgs<Session> e)
        {
            IClientChannel channel = (IClientChannel)sender;

            var message = new Message()
            {
                Content = new TextContent()
                {
                    Text = "Hello!"
                }
            };

            await channel.SendMessageAsync(message);

        }

        private static void ClientChannel_NotificationReceived(object sender, EnvelopeEventArgs<Notification> e)
        {
        }

        private static void ClientChannel_CommandReceived(object sender, EnvelopeEventArgs<Command> e)
        {
        }

        private static void ClientChannel_MessageReceived(object sender, EnvelopeEventArgs<Message> e)
        {
        }

        private static void ClientChannel_SessionFinished(object sender, EnvelopeEventArgs<Session> e)
        {

        }

        private static void ClientChannel_SessionFailed(object sender, EnvelopeEventArgs<Session> e)
        {

        }

        #endregion

        #region Server Event Handlers

        private static void Listener_Connected(object sender, TransportEventArgs e)
        {
            var serverChannel = new ServerChannel(
                Guid.NewGuid(),
                new Node() { Name = "server", Domain = IPGlobalProperties.GetIPGlobalProperties().DomainName, Instance = Environment.MachineName },
                e.Transport,
                TimeSpan.FromSeconds(60));

            _serverConnectedNodesDictionary.Add(serverChannel.SessionId, serverChannel);

            serverChannel.NewSessionReceived += ServerChannel_NewSessionReceived;
            serverChannel.AuthenticateSessionReceived += ServerChannel_AuthenticateSessionReceived;
            serverChannel.NegotiateSessionReceived += ServerChannel_NegotiateSessionReceived;
            serverChannel.MessageReceived += ServerChannel_MessageReceived;
            serverChannel.CommandReceived += ServerChannel_CommandReceived;
            serverChannel.NotificationReceived += ServerChannel_NotificationReceived;
            serverChannel.FinishingSessionReceived += ServerChannel_FinishingSessionReceived;

            serverChannel.Transport.Failed += Transport_Failed;
            serverChannel.Transport.Closing += Transport_Closing;
            serverChannel.Transport.Closed += Transport_Closed;

            serverChannel.Transport.OpenAsync(
                _listenerUri,
                CancellationToken.None);
        }

        private async static void ServerChannel_NewSessionReceived(object sender, EnvelopeEventArgs<Session> e)
        {
            IServerChannel channel = (IServerChannel)sender;

            await channel.SendNegotiatingSessionAsync(                
                channel.Transport.GetSupportedCompression(),
                channel.Transport.GetSupportedEncryption()
                );
        }

        private async static void ServerChannel_NegotiateSessionReceived(object sender, EnvelopeEventArgs<Session> e)
        {
            IServerChannel channel = (IServerChannel)sender;

            await channel.SendNegotiatingSessionAsync(
                e.Envelope.Compression.Value,
                e.Envelope.Encryption.Value
                );

            if (channel.Transport.Compression != e.Envelope.Compression.Value)
            {
                await channel.Transport.SetCompressionAsync(e.Envelope.Compression.Value, CancellationToken.None);                
            }

            if (channel.Transport.Encryption != e.Envelope.Encryption.Value)
            {
                await channel.Transport.SetEncryptionAsync(e.Envelope.Encryption.Value, CancellationToken.None);
            }

            await channel.SendAuthenticatingSessionAsync(
                new AuthenticationScheme[] { AuthenticationScheme.Guest, AuthenticationScheme.Plain });
        }

        private static async void ServerChannel_AuthenticateSessionReceived(object sender, EnvelopeEventArgs<Session> e)
        {
            IServerChannel channel = (IServerChannel)sender;

            if (e.Envelope.From != null &&
                e.Envelope.From.Name.Equals(Environment.UserName, StringComparison.CurrentCultureIgnoreCase))
            {
                _serverNodeSessionIdDictionary.Add(e.Envelope.From, channel.SessionId);

                await channel.SendEstablishedSessionAsync(e.Envelope.From, SessionMode.Node);
            }
            else
            {
                await channel.SendFailedSessionAsync(
                    new Reason()
                    {
                        Code = ReasonCodes.SESSION_AUTHENTICATION_FAILED,
                        Description = "Invalid user"
                    });
            }
        }

        private static async void ServerChannel_MessageReceived(object sender, EnvelopeEventArgs<Message> e)
        {
            IServerChannel channel = (IServerChannel)sender;

            if (channel.State == SessionState.Established)
            {
                var message = e.Envelope;
                if (message.From == null)
                {
                    message.From = channel.RemoteNode;
                }

                if (message.To == null)
                {
                    var notification = new Notification()
                    {
                        Id = message.Id,
                        Event = Event.Failed,
                        Reason = new Reason()
                        {
                            Code = ReasonCodes.VALIDATION_INVALID_DESTINATION,
                            Description = "Invalid destination"
                        }
                    };

                    await channel.SendNotificationAsync(notification);
                }
                else if (!_serverNodeSessionIdDictionary.ContainsKey(message.To))
                {
                    var notification = new Notification()
                    {
                        Id = message.Id,
                        Event = Event.Failed,
                        Reason = new Reason()
                        {
                            Code = ReasonCodes.DISPATCHING_DESTINATION_NOT_FOUND,
                            Description = "Destination not found"
                        }
                    };

                    await channel.SendNotificationAsync(notification);
                }
                else
                {
                    Guid destinationSessionId = _serverNodeSessionIdDictionary[message.To];
                    IServerChannel destinationChannel;

                    if (!_serverConnectedNodesDictionary.TryGetValue(destinationSessionId, out destinationChannel))
                    {
                        var notification = new Notification()
                        {
                            Id = message.Id,
                            Event = Event.Failed,
                            Reason = new Reason()
                            {
                                Code = ReasonCodes.DISPATCHING_ERROR,
                                Description = "Destination session is unavailable"
                            }
                        };

                        await channel.SendNotificationAsync(notification);
                    }
                    else
                    {
                        await destinationChannel.SendMessageAsync(message);
                    }                    
                }
            }
        }

        private static void ServerChannel_NotificationReceived(object sender, EnvelopeEventArgs<Notification> e)
        {

        }

        private static void ServerChannel_CommandReceived(object sender, EnvelopeEventArgs<Command> e)
        {

        }

        private static void ServerChannel_FinishingSessionReceived(object sender, EnvelopeEventArgs<Session> e)
        {


        }


        private static async void Transport_Failed(object sender, ExceptionEventArgs e)
        {
            using (e.GetDeferral())
            {

            }

            await ((ITransport)sender).CloseAsync(CancellationToken.None);
        }

        private static void Transport_Closing(object sender, DeferralEventArgs e)
        {
            using (e.GetDeferral())
            {

            }
        }


        private static void Transport_Closed(object sender, EventArgs e)
        {

        }

        #endregion
    }
}
