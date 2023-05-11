using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using Lime.Messaging;
using Lime.Protocol.Serialization;
using Lime.Protocol.Serialization.Newtonsoft;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;

namespace Lime.Protocol.LoadTests.Serialization;

public class DeserializeLoadTest
{
    [Test]
    public void DeserializeString()
    {
        var documentTypeResolver = new DocumentTypeResolver().WithMessagingDocuments();
        var serializer = new EnvelopeSerializer(documentTypeResolver);
        
        var n = 10000;
        var streamArray = new Stream[n];
        for (int i = 0; i < n; i++)
        {
            streamArray[i] = CreateStream();
        }

        var watch = Stopwatch.StartNew();
        for (int i = 0; i < n; i++)
        {
            using var reader = new StreamReader(streamArray[i]);
            var json = reader.ReadToEnd();
            serializer.Deserialize(json);
        }
        watch.Stop();
        Trace.WriteLine(watch.Elapsed);
    }

    [Test]
    public void DeserializeStream()
    {
        var documentTypeResolver = new DocumentTypeResolver().WithMessagingDocuments();
        var serializer = new EnvelopeSerializer(documentTypeResolver);
        
        var n = 1000000;
        var streamArray = new Stream[n];
        for (int i = 0; i < n; i++)
        {
            streamArray[i] = CreateStream();
        }

        var watch = Stopwatch.StartNew();
        for (int i = 0; i < n; i++)
        {
            using var reader = new StreamReader(streamArray[i]);
            serializer.Deserialize<Message>(reader);
        }
        watch.Stop();
        Trace.WriteLine(watch.Elapsed);
    }

    private Stream CreateStream()
    {
        var memoryStream = new MemoryStream();
        var writer = new StreamWriter(memoryStream);
        
        var json = "{\"id\":\"a77fa426-2990-4b98-adbf-db897436017b\",\"to\":\"949839515125748@messenger.gw.msging.net\",\"type\":\"application/vnd.lime.document-select+json\",\"content\":{\"header\":{\"type\":\"text/plain\",\"value\":\"Envie sua localizacao\"},\"options\":[{\"label\":{\"type\":\"application/vnd.lime.input+json\",\"value\":{\"validation\":{\"rule\":\"type\",\"type\":\"application/vnd.lime.location+json\"}}}}]}}";
        
        writer.Write(json);
        writer.Flush();
        memoryStream.Position = 0;
        return memoryStream;
    }
}