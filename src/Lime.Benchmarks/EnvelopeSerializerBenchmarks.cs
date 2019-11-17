using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Lime.Messaging;
using Lime.Protocol;
using Lime.Protocol.Serialization;
using Lime.Protocol.Serialization.Newtonsoft;
using Lime.Protocol.UnitTests;

namespace Lime.Benchmarks
{
    [CoreJob]
    [MemoryDiagnoser]
    public class EnvelopeSerializerBenchmarks
    {
        private readonly EnvelopeSerializer _envelopeSerializer;
        
        private Message _textMessage;
        private string _textMessageJson;
        private MemoryStream _textMessageStream;
        private MemoryStream _outputStream; 
        
        public EnvelopeSerializerBenchmarks()
        {
            _envelopeSerializer = new EnvelopeSerializer(new DocumentTypeResolver().WithMessagingDocuments());
        }
        
        [GlobalSetup]
        public void Setup()
        {
            _textMessage = Dummy.CreateMessage(Dummy.CreateTextContent());
            _textMessageJson = _envelopeSerializer.Serialize(_textMessage);
        }

        [IterationSetup]
        public void IterationSetup()
        {
            _textMessageStream = new MemoryStream(Encoding.UTF8.GetBytes(_textMessageJson))
            {
                Position = 0
            };
            _outputStream = new MemoryStream();
        }
        
        [Benchmark]
        public void SerializeTextMessage()
        {
            var json = _envelopeSerializer.Serialize(_textMessage);
        }

        [Benchmark]
        public void DeserializeTextMessage()
        {
            var message = _envelopeSerializer.Deserialize(_textMessageJson);
        }
        
        [Benchmark]
        public async Task SerializeTextMessageAsync()
        {
             await _envelopeSerializer.SerializeAsync(_textMessage, _outputStream, CancellationToken.None);
        }
        
        [Benchmark]
        public async Task DeserializeTextMessageAsync()
        {
            var message = await _envelopeSerializer.DeserializeAsync(_textMessageStream, CancellationToken.None);
        }

    }
}