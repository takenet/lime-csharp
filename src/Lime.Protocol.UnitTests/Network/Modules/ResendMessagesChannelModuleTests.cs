using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lime.Protocol.Network;
using Lime.Protocol.Network.Modules;
using Moq;
using NUnit.Framework;

namespace Lime.Protocol.UnitTests.Network.Modules
{
    [TestFixture]
    public class ResendMessagesChannelModuleTests
    {
        private Mock<IChannel> _channel;
        private int _resendMessageTryCount;
        private TimeSpan _resendMessageInterval;

        #region Scenario

        [SetUp]
        public void Setup()
        {
            _channel = new Mock<IChannel>();
            _resendMessageTryCount = 3;
            _resendMessageInterval = TimeSpan.FromMilliseconds(100);
        }

        [TearDown]
        public void Teardown()
        {
            _channel = null;            
        }

        #endregion

        public ResendMessagesChannelModule GetTarget()
        {
            return new ResendMessagesChannelModule(_channel.Object, _resendMessageTryCount, _resendMessageInterval);
        }
    }
}
