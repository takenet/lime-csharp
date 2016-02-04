using Lime.Messaging.Contents;
using Lime.Messaging.Resources;
using Lime.Protocol.Security;
using Lime.Protocol.Serialization;
using Lime.Protocol.UnitTests;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Shouldly;

namespace Lime.Protocol.UnitTests.Serialization
{
	[TestFixture]
    [Ignore]
	public class EnvelopeSerializerTests : EnvelopeSerializerBaseTests
	{
		protected override IEnvelopeSerializer GetTarget()
		{
			return new EnvelopeSerializer();
		}
	}
}