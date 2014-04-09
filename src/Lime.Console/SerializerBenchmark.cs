using Lime.Protocol;
using Lime.Protocol.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Console
{
    public static class SerializerBenchmark
    {

        public static void TestSerializers()
        {
            var authenticatingJson = "{\"state\":\"authenticating\",\"scheme\":\"plain\",\"authentication\":{\"password\":\"bXlwYXNzd29yZA==\"},\"id\":\"458f5c19-5655-47c9-8f67-a064c5f9f9d6\",\"from\":\"andreb@takenet.com.br/NOTEBIRES\"}";

            var json = authenticatingJson;

            var serializer1 = new EnvelopeSerializer();
            var serializer2 = new Lime.Protocol.Serialization.Newtonsoft.JsonNetSerializer();
            var serializer3 = new Lime.Protocol.Serialization.ServiceStack.ServiceStackSerializer();

            Envelope envelope1 = null, envelope2 = null, envelope3 = null;

            int count = 100000;

            System.Console.WriteLine("Deserialization:");


            var sw1 = System.Diagnostics.Stopwatch.StartNew();

            for (int i = 0; i < count; i++)
            {
                envelope1 = serializer1.Deserialize(json);
            }
            sw1.Stop();

            System.Console.WriteLine("EnvelopeSerializer: {0} ms", sw1.ElapsedMilliseconds);


            var sw2 = System.Diagnostics.Stopwatch.StartNew();

            for (int i = 0; i < count; i++)
            {
                envelope2 = serializer2.Deserialize(json);
            }
            sw2.Stop();

            System.Console.WriteLine("JsonNetSerializer: {0} ms", sw2.ElapsedMilliseconds);


            var sw3 = System.Diagnostics.Stopwatch.StartNew();

            for (int i = 0; i < count; i++)
            {
                envelope3 = serializer3.Deserialize(json);
            }
            sw3.Stop();

            System.Console.WriteLine("ServiceStackSerializer: {0} ms", sw3.ElapsedMilliseconds);

            var json1 = serializer2.Serialize(envelope1);
            var json2 = serializer2.Serialize(envelope1);
            var json3 = serializer2.Serialize(envelope1);

            if (json1 == json2 && json2 == json3)
            {
                System.Console.WriteLine("All deserialized types are equals");
            }
            else
            {
                System.Console.WriteLine("Deserialized types NOT equals: ");
            }

            var envelope = envelope1;

            System.Console.WriteLine("Serialization:");


            sw1 = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
            {
                json1 = serializer1.Serialize(envelope);
            }
            sw1.Stop();

            System.Console.WriteLine("EnvelopeSerializer: {0} ms", sw1.ElapsedMilliseconds);


            sw2 = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
            {
                json2 = serializer2.Serialize(envelope);
            }
            sw2.Stop();

            System.Console.WriteLine("JsonNetSerializer: {0} ms", sw2.ElapsedMilliseconds);


            sw3 = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
            {
                json3 = serializer3.Serialize(envelope);
            }
            sw3.Stop();

            System.Console.WriteLine("ServiceStackSerializer: {0} ms", sw3.ElapsedMilliseconds);

            if (json1 == json2 && json2 == json3)
            {
                System.Console.WriteLine("All serialized types are equals");
            }
            else
            {
                System.Console.WriteLine("Serialized types NOT equals: ");
            }

            System.Console.Read();
        }
    }
}
