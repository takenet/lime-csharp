using System;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;

namespace Lime.Transport.Http
{
    public class ClientHttpTransport : TransportBase
    {
        private readonly IHttpClient _httpClient;
        private readonly IEnvelopeSerializer _envelopeSerializer;

        public ClientHttpTransport(IHttpClient httpClient, IEnvelopeSerializer envelopeSerializer)
        {
            _httpClient = httpClient;
            _envelopeSerializer = envelopeSerializer;
        }

        public override Task SendAsync(Envelope envelope, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override Task<Envelope> ReceiveAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override bool IsConnected { get; }
        protected override Task PerformCloseAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected override Task PerformOpenAsync(Uri uri, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
   }

    public interface ISessionTokenProvider
    {
        Task<string> CreateTokenAsync(Session session, CancellationToken cancellationToken);
        
        Task<Session> GetSessionAsync(string token, CancellationToken cancellationToken);

        Task InvalidateTokenAsync(string token, CancellationToken cancellationToken);
    }
}