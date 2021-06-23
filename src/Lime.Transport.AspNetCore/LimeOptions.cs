using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Lime.Protocol;
using Lime.Protocol.Security;

namespace Lime.Transport.AspNetCore
{
    public class LimeOptions
    {
        public TransportEndPoint[] EndPoints { get; set; } = new[]
        {
            new TransportEndPoint()
            {
                Transport = TransportType.Tcp,
                EndPoint = new IPEndPoint(IPAddress.Any, 55321)
            },
            new TransportEndPoint()
            {
                Transport = TransportType.Ws,
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

    public enum TransportType
    {
        Tcp,
        Ws,
        Http
    }

    public class TransportEndPoint
    {
        public TransportType Transport { get; set; } = TransportType.Tcp;
        public IPEndPoint EndPoint { get; set; } = new IPEndPoint(IPAddress.Any, 55321);
        public bool Tls { get; set; } = false;
        public X509Certificate2? ServerCertificate { get; set; }
    }
}