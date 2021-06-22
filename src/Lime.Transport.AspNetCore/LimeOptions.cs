using System;
using System.Net;
using Lime.Protocol;
using Lime.Protocol.Security;
using Lime.Transport.WebSocket;

namespace Lime.Transport.AspNetCore
{
    public class LimeOptions
    {
        public ProtocolEndPoint[] EndPoints { get; set; } = new[]
        {
            new ProtocolEndPoint()
            {
                Protocol = Uri.UriSchemeNetTcp,
                EndPoint = new IPEndPoint(IPAddress.Any, 55321)
            },
            new ProtocolEndPoint()
            {
                Protocol = WebSocketTransportListener.UriSchemeWebSocket,
                EndPoint = new IPEndPoint(IPAddress.Any, 8080)
            },
        };

        public Node LocalNode { get; set; } =
            new Node(Environment.UserName, Environment.UserDomainName, Environment.MachineName);

        public TimeSpan SendTimeout { get; set; } = TimeSpan.FromSeconds(60);
        public SessionCompression[] EnabledCompressionOptions { get; set; } = {SessionCompression.None};

        public SessionEncryption[] EnabledEncryptionOptions { get; set; } =
            {SessionEncryption.None, SessionEncryption.TLS};

        public AuthenticationScheme[] SchemeOptions { get; set; } = {AuthenticationScheme.Guest};
    }

    public class ProtocolEndPoint
    {
        public string Protocol { get; set; } = Uri.UriSchemeNetTcp;
        public IPEndPoint EndPoint { get; set; } = new IPEndPoint(IPAddress.Any, 55321);

        public void Deconstruct(out string protocol, out IPEndPoint endPoint)
        {
            protocol = Protocol;
            endPoint = EndPoint;
        }
    }
}