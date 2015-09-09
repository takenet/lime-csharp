using Lime.Protocol.UnitTests.Serialization;
using NUnit.Framework;

namespace Lime.Protocol.Serialization.Newtonsoft.UnitTests
{
    [TestFixture]
    public class JsonNetSerializerTests : EnvelopeSerializerTests
    {
        public override IEnvelopeSerializer GetTarget()
        {
            return new JsonNetSerializer();
        }
   }
}