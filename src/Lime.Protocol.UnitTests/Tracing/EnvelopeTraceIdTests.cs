using System.Collections.Generic;
using Lime.Protocol.Tracing;
using NUnit.Framework;
using Shouldly;

namespace Lime.Protocol.UnitTests.Tracing
{
    [TestFixture]
    public class EnvelopeTraceIdTests
    {
        private const string TraceId = "0af7651916cd43dd8448eb211c80319c";

        [Test]
        public void GetTraceId_ValidTraceParent_ReturnsTraceId()
        {
            var message = new Message
            {
                Metadata = new Dictionary<string, string>
                {
                    [TraceContext.TraceParent] = $"00-{TraceId}-b7ad6b7169203331-01"
                }
            };

            message.GetTraceId().ShouldBe(TraceId);
        }

        [Test]
        public void GetTraceId_NoMetadata_ReturnsNull()
        {
            new Message().GetTraceId().ShouldBeNull();
            ((Envelope)null).GetTraceId().ShouldBeNull();
        }

        [TestCase("")]
        [TestCase("garbage")]
        [TestCase("00-00000000000000000000000000000000-b7ad6b7169203331-01")]
        [TestCase("00-0AF7651916CD43DD8448EB211C80319C-b7ad6b7169203331-01")]
        [TestCase("00-0af7651916cd43dd8448eb211c80319x-b7ad6b7169203331-01")]
        public void GetTraceId_InvalidTraceParent_ReturnsNull(string traceParent)
        {
            var message = new Message
            {
                Metadata = new Dictionary<string, string> { [TraceContext.TraceParent] = traceParent }
            };

            message.GetTraceId().ShouldBeNull();
        }
    }
}
