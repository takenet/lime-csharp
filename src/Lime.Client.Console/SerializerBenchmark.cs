using Lime.Protocol;
using Lime.Protocol.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Client.Console
{
    public static class SerializerBenchmark
    {

        public static void TestSerializers()
        {
            //var authenticatingJson = "{\"state\":\"authenticating\",\"scheme\":\"plain\",\"authentication\":{\"password\":\"bXlwYXNzd29yZA==\"},\"id\":\"458f5c19-5655-47c9-8f67-a064c5f9f9d6\",\"from\":\"andreb@takenet.com.br/NOTEBIRES\"}";

            var commandJson = "{\"type\":\"application/vnd.lime.roster+json\",\"resource\":{\"contacts\":[{\"identity\":\"qcux04xm@vs90fxcdy1.com\",\"name\":\"gbt9g5eioc9dqkb86sonim2sjvhekexokftjhr6zsrpvnscyzh\",\"isPending\":true,\"shareAccountInfo\":false,\"sharePresence\":true},{\"identity\":\"1uzlt4jb@0vya8kov3k.com\",\"name\":\"110pwz406hf0ssodndz9wyag1kartfh1z177ql79q28c69cgv4\",\"sharePresence\":false},{\"identity\":\"6k63yd0u@ylymh1anb0.com\",\"name\":\"uqxbfzt3z8k1i46yi655d4h5xwldvgbs7lveh5egk4n2k6c3qs\",\"isPending\":true,\"sharePresence\":false}]},\"method\":\"get\",\"status\":\"success\",\"id\":\"b625b0f9-c187-4858-a79b-d9b82088ad3e\",\"from\":\"tdrfx0j1@otopis6147.com/j1rn1\",\"pp\":\"v55tcfuz@bfl3unj5gx.com/963f4\",\"to\":\"yujqov1t@rwcx8b2y54.com/pqv25\",\"metadata\":{\"randomString1\":\"70bjn6b6g6zxvscyn77brxihkv9j3v5bpa73a6g39je640yme8\",\"randomString2\":\"v95emks48261dvaxhvuiyr0dbislmu7wf495glxwlf7o6ift5g\"}}";

            var json = commandJson;

            var serializer1 = new EnvelopeSerializer();
            var serializer2 = new Lime.Protocol.Serialization.Newtonsoft.JsonNetSerializer();
            var serializer3 = new Lime.Protocol.Serialization.ServiceStack.ServiceStackSerializer();


            Envelope envelope1 = null, envelope2 = null, envelope3 = null;

            int count;

            do
            {
                System.Console.Write("Serialization count: ");
            } while (!int.TryParse(System.Console.ReadLine(), out count));
                        
            System.Console.WriteLine("Deserialization:");

            envelope1 = ExecuteDeserialization(serializer1, json, count);
            envelope2 = ExecuteDeserialization(serializer2, json, count);
            envelope3 = ExecuteDeserialization(serializer3, json, count);

            // netwonsoft is the reference serializer
            var json1 = serializer2.Serialize(envelope1);
            var json2 = serializer2.Serialize(envelope2);
            var json3 = serializer2.Serialize(envelope3);

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

            json1 = ExecuteSerialization(serializer1, envelope, count);
            json2 = ExecuteSerialization(serializer2, envelope, count);
            json3 = ExecuteSerialization(serializer3, envelope, count);

            if (json1 == json2 && json2 == json3)
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
