using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Serialization;
using Lime.Protocol.Serialization.Newtonsoft;
using Lime.Protocol.UnitTests;
using Lime.Transport.Tcp;
using Shouldly;
using Xunit;

namespace Lime.Protocol.LoadTests
{

    public sealed class FakeEnvelopeSerializer : IEnvelopeSerializer
    {
        private readonly Envelope[] _envelopes;
        private readonly string[] _serializedEnvelopes;
        private int _serializePos;
        private int _deserializePos;

        private readonly object _serializeSyncRoot = new object();
        private readonly object _deserializeSyncRoot = new object();

        public FakeEnvelopeSerializer(int count)
        {
            _envelopes = Enumerable
                .Range(0, count)
                .Select<int, Envelope>(i =>
                {
                    if (i % 5 == 0)
                    {
                        return Dummy.CreateNotification(Event.Received);
                    }
                    if (i % 3 == 0)
                    {
                        return Dummy.CreateCommand(Dummy.CreateAccount());
                    }
                    if (i % 2 == 0)
                    {
                        return Dummy.CreateMessage(Dummy.CreateTextContent());
                    }
                    return Dummy.CreateMessage(Dummy.CreateSelect());
                })
                .ToArray();

            var jsonSerializer = new JsonNetSerializer();
            _serializedEnvelopes = _envelopes.Select(e => jsonSerializer.Serialize(e)).ToArray();
        }

        public string Serialize(Envelope envelope)
        {
            lock (_serializeSyncRoot)
            {
                var value = _serializedEnvelopes[_serializePos];
                _serializePos = (_serializedEnvelopes.Length + 1) % _serializedEnvelopes.Length;
                return value;
            }
        }

        public Envelope Deserialize(string envelopeString)
        {
            lock (_deserializeSyncRoot)
            {
                var value = _envelopes[_deserializePos];
                _deserializePos = (_envelopes.Length + 1) % _envelopes.Length;
                return value;
            }
        }
    }
}
