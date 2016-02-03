using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lime.Protocol.Adapters;
using Lime.Protocol.Network;
using Moq;
using NUnit.Core;
using NUnit.Framework;

namespace Lime.Protocol.UnitTests.Adapters
{
    [TestFixture]
    public class ChannelListenerAdapterBaseTests
    {
        private Mock<IMessageChannel> _messageChannel;
        private Mock<INotificationChannel> _notificationChannel;
        private Mock<ICommandChannel> _commandChannel;

        [SetUp]
        public void Setup()
        {
            _messageChannel = new Mock<IMessageChannel>();
            _notificationChannel = new Mock<INotificationChannel>();
            _commandChannel = new Mock<ICommandChannel>();
        }
    }

    public class TestChannelListenerAdapterBase : ChannelListenerAdapterBase
    {
        public TestChannelListenerAdapterBase(IMessageChannel messageChannel, INotificationChannel notificationChannel, ICommandChannel commandChannel) 
            : base(messageChannel, notificationChannel, commandChannel)
        {
        }

        public new void StartListenerTasks(Func<Message, Task> messageConsumer, Func<Notification, Task> notificationConsumer, Func<Command, Task> commandConsumer)
        {
            base.StartListenerTasks(messageConsumer, notificationConsumer, commandConsumer);
        }
    }
}
