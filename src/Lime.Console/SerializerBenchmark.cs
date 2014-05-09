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
            var serializer2 = new EnvelopeSerializer2();
            var serializer3 = new Lime.Protocol.Serialization.Newtonsoft.JsonNetSerializer();
            var serializer4 = new Lime.Protocol.Serialization.ServiceStack.ServiceStackSerializer();


            Envelope envelope1 = null, envelope2 = null, envelope3 = null, envelope4 = null;

            int count;

            do
            {
                System.Console.Write("Serialization count: ");
            } while (!int.TryParse(System.Console.ReadLine(), out count));

                        

            System.Console.WriteLine("Deserialization:");

            envelope1 = ExecuteDeserialization(serializer1, json, count);
            envelope2 = ExecuteDeserialization(serializer2, json, count);
            envelope3 = ExecuteDeserialization(serializer3, json, count);
            envelope4 = ExecuteDeserialization(serializer4, json, count);

            // netwonsoft is the reference serializer
            var json1 = serializer3.Serialize(envelope1);
            var json2 = serializer3.Serialize(envelope2);
            var json3 = serializer3.Serialize(envelope3);
            var json4 = serializer3.Serialize(envelope4);

            if (json1 == json2 && json2 == json3 && json3 == json4)
            {
                System.Console.WriteLine("All deserialized types are equals");
            }
            else
            {
                System.Console.WriteLine("Deserialized types NOT equals: ");
            }

            var envelope = envelope1;

            System.Console.WriteLine("Serialization:");

            json1 = ExecuteSerialization(serializer1, envelope, count);
            json2 = ExecuteSerialization(serializer2, envelope, count);
            json3 = ExecuteSerialization(serializer3, envelope, count);
            json4 = ExecuteSerialization(serializer4, envelope, count);

            if (json1 == json2 && json2 == json3 && json3 == json4)
            {
                System.Console.WriteLine("All serialized types are equals");
            }
            else
            {
                System.Console.WriteLine("Serialized types are NOT equals: ");
            }

            System.Console.Read();
        }

        public static string ExecuteSerialization(IEnvelopeSerializer serializer, Envelope envelope, int count)
        {
            string json = null;

            var sw = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
            {
                json = serializer.Serialize(envelope);
            }
            sw.Stop();

            System.Console.WriteLine("{0}: {1} ms", serializer.GetType().Name, sw.ElapsedMilliseconds);

            return json;
        }

        public static Envelope ExecuteDeserialization(IEnvelopeSerializer serializer, string json, int count)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            Envelope envelope = null;

            for (int i = 0; i < count; i++)
            {
                envelope = serializer.Deserialize(json);
            }
            sw.Stop();

            System.Console.WriteLine("{0}: {1} ms", serializer.GetType().Name, sw.ElapsedMilliseconds);

            return envelope;

        }
    }
}
