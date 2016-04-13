using Lime.Protocol.Serialization;
using Lime.Protocol.Serialization.Newtonsoft;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Shouldly;

namespace Lime.Protocol.UnitTests.Serialization.Newtonsoft
{
    [TestFixture]
    public class JsonNetSerializerTests : EnvelopeSerializerBaseTests
    {
        private static readonly global::Newtonsoft.Json.JsonSerializer JsonSerializer = global::Newtonsoft.Json.JsonSerializer.Create(JsonNetSerializer.Settings);

        protected override IEnvelopeSerializer GetTarget()
        {            
            return new JsonNetSerializer();
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_CommandWithMessage_ReturnsValidInstance()
        {
            // Arrange
            var json =
                "{\"uri\":\"/sessions/92d8f9fb-0857-4b1a-8f50-f0f37dedc140?expiration=635961514943253147\",\"type\":\"application/vnd.lime.message+json\",\"resource\":{\"id\":\"52e7804c-e483-4f65-85e2-52abc007b35b\",\"from\":\"andreb@msging.net/default\",\"to\":\"joao@msging.net\",\"type\":\"text/plain\",\"content\":\"Banana\",\"metadata\":{\"$internalId\":\"66ec04ec-a278-4251-8ff3-6931bf73e04f\"}},\"method\":\"set\",\"id\":\"66ec04ec-a278-4251-8ff3-6931bf73e04f\"}";

            var target = GetTarget();

            // Act
            var actual = target.Deserialize(json);

            // Assert
            var actualCommand = actual.ShouldBeOfType<Command>();
            actualCommand.Resource.ShouldNotBeNull();
            var jsonDocument = actualCommand.Resource.ShouldBeOfType<JsonDocument>();
            var jObject = JObject.FromObject(jsonDocument, JsonSerializer);
            var message = jObject.ToObject(typeof(Message), JsonSerializer).ShouldBeOfType<Message>();                        
        }
    }
}