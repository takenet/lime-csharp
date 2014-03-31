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


        static void Main(string[] args)
        {
            _serverConnectedNodesDictionary = new Dictionary<Guid, IServerChannel>();
            _serverNodeSessionIdDictionary = new Dictionary<Node, Guid>();

            _listenerUri = new Uri("net.tcp://localhost:55321");

            var listener = StartListenerAsync().Result;

            var clientChannel = StartClientAsync().Result;

            System.Console.ReadLine();

            clientChannel.SendFinishingSessionAsync().Wait();

        }

        private static async Task<ITransportListener> StartListenerAsync()
        {
            ITransportListener listener = new TcpTransportListener(
                new JsonNetSerializer(),
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
                tcpClient,
                new JsonNetSerializer(),
                traceWriter: new DebugTraceWriter("Client")
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

        #region Client Event Handlers


        private async static void ClientChannel_NegotiateSessionReceived(object sender, EnvelopeEventArgs<Session> e)
        {
            IClientChannel channel = (IClientChannel)sender;

            if (e.Envelope.CompressionOptions != null ||
                e.Envelope.EncryptionOptions != null)
            {
                await channel.SendNegotiatingSessionAsync(e.Envelope.CompressionOptions.First(), e.Envelope.EncryptionOptions.First());
            }
            else if (e.Envelope.Encryption.HasValue && e.Envelope.Compression.HasValue)
            {
                if (channel.Transport.Compression != e.Envelope.Compression.Value)
                {
                    await channel.Transport.SetEncryptionAsync(e.Envelope.Encryption.Value, CancellationToken.None);
                }

                if (channel.Transport.Encryption != e.Envelope.Encryption.Value)
                {
                    await channel.Transport.SetCompressionAsync(e.Envelope.Compression.Value, CancellationToken.None);
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
                await channel.Transport.SetEncryptionAsync(e.Envelope.Encryption.Value, CancellationToken.None);
            }

            if (channel.Transport.Encryption != e.Envelope.Encryption.Value)
            {
                await channel.Transport.SetCompressionAsync(e.Envelope.Compression.Value, CancellationToken.None);
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

        private static void ServerChannel_MessageReceived(object sender, EnvelopeEventArgs<Message> e)
        {
            IServerChannel channel = (IServerChannel)sender;

            if (channel.State == SessionState.Established)
            {



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
