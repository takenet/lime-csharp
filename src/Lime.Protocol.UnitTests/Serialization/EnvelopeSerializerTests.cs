using Lime.Protocol.Serialization;
using NUnit.Framework;

namespace Lime.Protocol.UnitTests.Serialization
{
	[TestFixture]
    [Ignore("Deprecated class")]
	public class EnvelopeSerializerTests : EnvelopeSerializerBaseTests
	{
		protected override IEnvelopeSerializer GetTarget()
		{
			return new EnvelopeSerializer();
		}
	}
}