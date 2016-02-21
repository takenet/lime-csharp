using Lime.Protocol.UnitTests.Serialization;

namespace Lime.Protocol.Serialization.Newtonsoft.UnitTests
{
    public class JsonNetSerializerTests : EnvelopeSerializerBaseTests
    {
        protected override IEnvelopeSerializer GetTarget()
        {
            return new JsonNetSerializer();
        }
   }
}