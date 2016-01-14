using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Client;

namespace Lime.Protocol.Network.Modules
{
    public sealed class FillEnvelopeRecipientsChannelModule<T> : ChannelModuleBase<T> where T : Envelope, new()
    {
        private readonly IChannel _channel;

        public FillEnvelopeRecipientsChannelModule(IChannel channel)
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            _channel = channel;
        }

        public override Task<T> OnSending(T envelope, CancellationToken cancellationToken)
        {
            if (_channel is ClientChannel &&
                _channel.LocalNode != null)
            {
                if (envelope.Pp == null)
                {
                    if (envelope.From != null &&
                        !envelope.From.Equals(_channel.LocalNode))
                    {
                        envelope.Pp = _channel.LocalNode.Copy();
                    }
                }
                else if (string.IsNullOrWhiteSpace(envelope.Pp.Domain))
                {
                    envelope.Pp.Domain = _channel.LocalNode.Domain;
                }
            }

            return envelope.AsCompletedTask();
        }


        public override Task<T> OnReceiving(T envelope, CancellationToken cancellationToken)
        {
            var from = _channel.RemoteNode;
            var to = _channel.LocalNode;

            if (from != null)
            {
                if (envelope.From == null)
                {
                    envelope.From = from.Copy();
                }
                else if (string.IsNullOrEmpty(envelope.From.Domain))
                {
                    envelope.From.Domain = from.Domain;
                }
            }

            if (to != null)
            {
                if (envelope.To == null)
                {
                    envelope.To = to.Copy();
                }
                else if (string.IsNullOrEmpty(envelope.To.Domain))
                {
                    envelope.To.Domain = to.Domain;
                }
            }

            return envelope.AsCompletedTask();
        }
    }
}
