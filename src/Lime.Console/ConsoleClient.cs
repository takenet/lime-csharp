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

            var resultSession = await Channel.EstablishSessionAsync(
                c => c.First(),
                e => e.First(),
                identity,
                (s, r) => { var auth = new PlainAuthentication(); auth.SetToBase64Password(password); return auth; },
                Environment.MachineName,
                SessionMode.Node,
                cancellationToken);

            if (resultSession.State != SessionState.Established)            
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
