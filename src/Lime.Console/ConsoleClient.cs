using Lime.Protocol;
using Lime.Protocol.Client;
using Lime.Protocol.Network;
using Lime.Protocol.Security;
using Lime.Protocol.Serialization;
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
    public class ConsoleClient
    {
        private static Uri _clientUri;
        public IClientChannel Channel { get; private set; }

        public ConsoleClient(Uri clientUri)
        {
            _clientUri = clientUri;
        }

        public async Task<bool> ConnectAsync(Identity identity, string password, CancellationToken cancellationToken)
        {
            var tcpClient = new TcpClient();

#if DEBUG
            ITraceWriter traceWriter = new DebugTraceWriter("Client"); 
#else
            ITraceWriter traceWriter = new FileTraceWriter("client.log");
#endif


            var transport = new TcpTransport(
                new TcpClientAdapter(tcpClient),
                new EnvelopeSerializer(),                
                hostName: _clientUri.Host,
                traceWriter: traceWriter
                );

            Channel = new ClientChannel(transport, TimeSpan.FromSeconds(60));
            
            await Channel.Transport.OpenAsync(_clientUri, cancellationToken);

            var negotiateOrAuthenticateSession = await Channel.StartNewSessionAsync(cancellationToken);

            Session authenticatingSession;

            if (negotiateOrAuthenticateSession.State == SessionState.Negotiating)
            {
                var confirmedNegotiateSession = await Channel.NegotiateSessionAsync(negotiateOrAuthenticateSession.CompressionOptions.First(), negotiateOrAuthenticateSession.EncryptionOptions.Last(), cancellationToken);

                if (Channel.Transport.Compression != confirmedNegotiateSession.Compression.Value)
                {
                    await Channel.Transport.SetCompressionAsync(
                        confirmedNegotiateSession.Compression.Value,
                        cancellationToken);
                }

                if (Channel.Transport.Encryption != confirmedNegotiateSession.Encryption.Value)
                {
                    await Channel.Transport.SetEncryptionAsync(
                        confirmedNegotiateSession.Encryption.Value, 
                        cancellationToken);
                }

                authenticatingSession = await Channel.ReceiveAuthenticatingSessionAsync(cancellationToken);
            }
            else
            {
                authenticatingSession = negotiateOrAuthenticateSession;
            }
            
            if (authenticatingSession.State != SessionState.Authenticating)
            {
                return false;
            }

            var authentication = new PlainAuthentication();
            authentication.SetToBase64Password(password);

            var authenticationResultSession = await Channel.AuthenticateSessionAsync(
                new Identity() { Name = identity.Name, Domain = identity.Domain },
                authentication,
                Environment.MachineName,
                SessionMode.Node,
                cancellationToken);

            if (authenticationResultSession.State != SessionState.Established)            
            {
                return false;
            }

            return true;
        }

        public async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
        {
            while (Channel.State == SessionState.Established)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var message = await Channel.ReceiveMessageAsync(cancellationToken);
                                
                System.Console.WriteLine("Message received from '{0}': {1}", message.From, message.Content);                
            }
        }

        public async Task ReceiveNotificationsAsync(CancellationToken cancellationToken)
        {
            while (Channel.State == SessionState.Established)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var notification = await Channel.ReceiveNotificationAsync(cancellationToken);

                if (notification.Reason != null)
                {
                    System.Console.WriteLine("Notification received from '{0}': {1} - Reason: {2}", notification.From, notification.Event, notification.Reason.Description);
                }
                else
                {
                    System.Console.WriteLine("Notification received from '{0}': {1}", notification.From, notification.Event);
                }                
            }
        }

        public async Task Disconnect(CancellationToken cancellationToken)
        {
            await Channel.SendFinishingSessionAsync();
            await Channel.ReceiveFinishedSessionAsync(cancellationToken);
        }

    }
}
