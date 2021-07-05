using NUnit.Framework;
using Shouldly;

namespace Lime.Transport.AspNetCore.UnitTests
{
    [TestFixture]
    public class HttpEndPointOptionsTests
    {
        private HttpEndPointOptions GetTarget() => new HttpEndPointOptions();
        
        [Test]
        public void IsValid_DistinctPaths_ReturnsTrue()
        {
            // Arrange
            var target = GetTarget();
            target.CommandsPath = "/commands";
            target.MessagesPath = "/messages";
            target.NotificationsPath = "/notifications";

            // Act
            var actual = target.IsValid();
            
            // Assert
            actual.ShouldBeTrue();
        }        
        
        [Test]
        public void IsValid_TwoEqualPaths_ReturnsFalse()
        {
            // Arrange
            var target = GetTarget();
            target.CommandsPath = "/envelopes";
            target.MessagesPath = "/envelopes";
            target.NotificationsPath = "/notifications";

            // Act
            var actual = target.IsValid();
            
            // Assert
            actual.ShouldBeFalse();
        }
        
        [Test]
        public void IsValid_AllEqualPaths_ReturnsFalse()
        {
            // Arrange
            var target = GetTarget();
            target.CommandsPath = "/envelopes";
            target.MessagesPath = "/envelopes";
            target.NotificationsPath = "/envelopes";

            // Act
            var actual = target.IsValid();
            
            // Assert
            actual.ShouldBeFalse();
        }
    }
}