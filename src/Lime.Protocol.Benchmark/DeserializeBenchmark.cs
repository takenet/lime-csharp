using BenchmarkDotNet.Attributes;
using Lime.Messaging;
using Lime.Protocol.Serialization;
using Lime.Protocol.Serialization.Newtonsoft;

namespace Lime.Protocol.Benchmark;

[MemoryDiagnoser]
[ThreadingDiagnoser]
public class DeserializeBenchmark
{
    private readonly string _json = "{\"id\":\"a77fa426-2990-4b98-adbf-db897436017b\",\"to\":\"949839515125748@messenger.gw.msging.net\",\"type\":\"application/vnd.lime.document-select+json\",\"content\":{\"header\":{\"type\":\"text/plain\",\"value\":\"Envie sua localizacao\"},\"options\":[{\"label\":{\"type\":\"application/vnd.lime.input+json\",\"value\":{\"validation\":{\"rule\":\"type\",\"type\":\"application/vnd.lime.location+json\"}}}}]}}";
    private readonly EnvelopeSerializer _envelopeSerializer;
    private Stream[] _deserializeStringInput;
    private Stream[] _deserializeStreamInput;
    private readonly int _n = 5000;
    
    public DeserializeBenchmark()
    {
        var documentTypeResolver = new DocumentTypeResolver().WithMessagingDocuments();
        _envelopeSerializer = new EnvelopeSerializer(documentTypeResolver);
        _deserializeStringInput = new Stream[_n];
        _deserializeStreamInput = new Stream[_n];
    }

    [Benchmark]
    public void DeserializeString()
    {
        for (int i = 0; i < _n; i++)
        {
            var reader = new StreamReader(_deserializeStringInput[i]);
            _envelopeSerializer.Deserialize(reader.ReadToEnd());
        }
    }
    
    [Benchmark]
    public void DeserializeStream()
    {
        for (int i = 0; i < _n; i++)
        {
            var reader = new StreamReader(_deserializeStreamInput[i]);
            _envelopeSerializer.Deserialize<Message>(reader);
        }
    }

    [IterationSetup]
    public void IterationSetup()
    {
        for (int i = 0; i < _n; i++)
        {
            _deserializeStringInput[i] = CreateStream();
            _deserializeStreamInput[i] = CreateStream();
        }
    }
    
    [IterationCleanup]
    public void IterationCleanUp()
    {
        for (int i = 0; i < _n; i++)
        {
            _deserializeStringInput[i].Dispose();
            _deserializeStreamInput[i].Dispose();
        }
    }
   
    private Stream CreateStream()
    {
        var memoryStream = new MemoryStream();
        var writer = new StreamWriter(memoryStream);
        writer.Write(_json);
        writer.Flush();
        memoryStream.Position = 0;
        return memoryStream;
    }
}