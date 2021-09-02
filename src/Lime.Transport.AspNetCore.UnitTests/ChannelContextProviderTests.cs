using System;
using Lime.Transport.AspNetCore.Transport;
using NUnit.Framework;
using Shouldly;

namespace Lime.Transport.AspNetCore.UnitTests
{
    [TestFixture]
    public class ChannelContextProviderTests : TestsBase
    {
        private ChannelContextProvider GetTarget() => new ChannelContextProvider();

        [SetUp]
        public void SetUp()
        {
            base.SetUp(new TransportEndPoint());
        }
        
        [Test]
        public void SetContext_ValidChannelContext_ShouldBeStored()
        {
            // Arrange
            var context = new ChannelContext(SenderChannel.Object, new ChannelProvider());
            var target = GetTarget();
            
            // Act
            target.SetContext(context);
            
            // Assert
            var actual = target.GetContext();
            actual.ShouldBe(context);
        }        

        [Test]
        public void SetContext_SetTwice_ThrowsInvalidOperation()
        {
            // Arrange
            var context = new ChannelContext(SenderChannel.Object, new ChannelProvider());
            var target = GetTarget();
            target.SetContext(context);
            
            // Act
            Assert.Throws<InvalidOperationException>(() => target.SetContext(context));
        }

        [Test]
        public void GetContext_NotSet_ThrowsInvalidOperation()
        {
            // Arrange
            var target = GetTarget();
            
            // Act
            Assert.Throws<InvalidOperationException>(() => target.GetContext());
        }
    }
}