using System;
using System.Buffers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Microsoft.AspNetCore.Http;

namespace Lime.Transport.AspNetCore
{
    /// <summary>
    /// Emulates a channel using the HttpContext to allow the application sending a envelope in the HTTP response.
    /// </summary>
    public sealed class HttpContextChannel : ISenderChannel
    {
        private readonly IEnvelopeSerializer _envelopeSerializer;
        private readonly SemaphoreSlim _sendSemaphore;
        private bool _envelopeSent;
        
        public HttpContextChannel(HttpContext context, Node localNode, Node remoteNode, IEnvelopeSerializer envelopeSerializer)
        {
            _envelopeSerializer = envelopeSerializer;
            Context = context;
            LocalNode = localNode;
            RemoteNode = remoteNode;
            SessionId = context.Connection.Id;
            State = SessionState.Established;
            Transport = new TransportInformation(
                SessionCompression.None,
                context.Request.IsHttps ? SessionEncryption.TLS : SessionEncryption.None,
                true,
                "",
                "", 
                null);
            _sendSemaphore = new SemaphoreSlim(1);
        }
        
        public HttpContext Context { get; }
        
        public string SessionId { get; }
        public SessionState State { get; }
        public Node LocalNode { get; }
        public Node RemoteNode { get; }

        public ITransportInformation Transport { get; }

        public Task SendMessageAsync(Message message, CancellationToken cancellationToken) =>
            SendEnvelopeAsync(message, cancellationToken);

        public Task SendNotificationAsync(Notification notification, CancellationToken cancellationToken) =>
            SendEnvelopeAsync(notification, cancellationToken);

        public Task SendCommandAsync(Command command, CancellationToken cancellationToken) =>
            SendEnvelopeAsync(command, cancellationToken);

        public Task<Command> ProcessCommandAsync(Command requestCommand, CancellationToken cancellationToken) => 
            Task.FromException<Command>(
                new NotSupportedException("ProcessCommand method is not supported on HTTP transport"));
        
        private async Task SendEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken)
        {
            EnsureNotSent();
            
            await _sendSemaphore.WaitAsync(cancellationToken);
            try
            {
                EnsureNotSent();
                _envelopeSent = true;
                
                Context.Response.ContentType = "application/json";
                var serializedEnvelope = _envelopeSerializer.Serialize(envelope);
                var buffer = ArrayPool<byte>.Shared.Rent(Encoding.UTF8.GetByteCount(serializedEnvelope));
                try
                {
                    var length = Encoding.UTF8.GetBytes(serializedEnvelope, 0, serializedEnvelope.Length, buffer, 0);
                    await Context.Response.Body.WriteAsync(buffer, 0, length, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
            finally
            {
                _sendSemaphore.Release();
            }
        }

        private void EnsureNotSent()
        {
            if (_envelopeSent)
            {
                throw new NotSupportedException("Only one envelope can be sent per request on HTTP transport");
            }
        }
    }
}