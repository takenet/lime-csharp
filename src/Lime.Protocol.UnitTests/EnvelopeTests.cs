using Lime.Protocol.Security;
using Xunit;
using Shouldly;

namespace Lime.Protocol.UnitTests
{
    
    public class EnvelopeTests
    {
        [Fact]
        [Trait("Category", "ShallowCopy")]
        public void ShallowCopy_PresenceRequestCommand_ReturnsClone()
        {
            // Arrange
            var command = Dummy.CreateCommand(Dummy.CreatePresence(), uri:Dummy.CreateRelativeLimeUri());
            command.Metadata = Dummy.CreateStringStringDictionary();

            // Act
            var actual = command.ShallowCopy();

            // Assert
            actual.ShouldNotBe(command);
            actual.Id.ShouldBe(command.Id);
            actual.From.ShouldBe(command.From);
            actual.To.ShouldBe(command.To);
            actual.Pp.ShouldBe(command.Pp);
            actual.Metadata.ShouldBe(command.Metadata);
            actual.Method.ShouldBe(command.Method);
            actual.Reason.ShouldBe(command.Reason);
            actual.Resource.ShouldBe(command.Resource);
            actual.Status.ShouldBe(command.Status);
            actual.Type.ShouldBe(command.Type);
            actual.Uri.ShouldBe(command.Uri);
        }

        [Fact]
        [Trait("Category", "ShallowCopy")]
        public void ShallowCopy_FailedResponseCommand_ReturnsClone()
        {
            // Arrange
            var command = Dummy.CreateCommand(method:CommandMethod.Delete, status:CommandStatus.Failure);
            command.Reason = Dummy.CreateReason();

            // Act
            var actual = command.ShallowCopy();

            // Assert
            actual.ShouldNotBe(command);
            actual.Id.ShouldBe(command.Id);
            actual.From.ShouldBe(command.From);
            actual.To.ShouldBe(command.To);
            actual.Pp.ShouldBe(command.Pp);
            actual.Metadata.ShouldBe(command.Metadata);
            actual.Method.ShouldBe(command.Method);
            actual.Reason.ShouldBe(command.Reason);
            actual.Resource.ShouldBe(command.Resource);
            actual.Status.ShouldBe(command.Status);
            actual.Type.ShouldBe(command.Type);
            actual.Uri.ShouldBe(command.Uri);
        }


        [Fact]
        [Trait("Category", "ShallowCopy")]
        public void ShallowCopy_TextMessage_ReturnsClone()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreateTextContent());

            // Act
            var actual = message.ShallowCopy();

            // Assert
            actual.ShouldNotBe(message);
            actual.Id.ShouldBe(message.Id);
            actual.From.ShouldBe(message.From);
            actual.To.ShouldBe(message.To);
            actual.Pp.ShouldBe(message.Pp);
            actual.Metadata.ShouldBe(message.Metadata);
            actual.Content.ShouldBe(message.Content);
            actual.Type.ShouldBe(message.Type);
        }

        [Fact]
        [Trait("Category", "ShallowCopy")]
        public void ShallowCopy_DispatchedNotification_ReturnsClone()
        {
            // Arrange
            var notification = Dummy.CreateNotification(Event.Received);

            // Act
            var actual = notification.ShallowCopy();

            // Assert
            actual.ShouldNotBe(notification);
            actual.Id.ShouldBe(notification.Id);
            actual.From.ShouldBe(notification.From);
            actual.To.ShouldBe(notification.To);
            actual.Pp.ShouldBe(notification.Pp);
            actual.Metadata.ShouldBe(notification.Metadata);
            actual.Event.ShouldBe(notification.Event);
        }
        [Fact]
        [Trait("Category", "ShallowCopy")]
        public void ShallowCopy_NegotiatingSession_ReturnsClone()
        {
            // Arrange
            var session = Dummy.CreateSession(SessionState.Negotiating);
            session.Authentication = Dummy.CreateAuthentication(AuthenticationScheme.Plain);
            session.Compression = SessionCompression.GZip;
            session.EncryptionOptions = new [] { SessionEncryption.None, SessionEncryption.TLS, };
            
            // Act
            var actual = session.ShallowCopy();

            // Assert
            actual.ShouldNotBe(session);
            actual.Id.ShouldBe(session.Id);
            actual.From.ShouldBe(session.From);
            actual.To.ShouldBe(session.To);
            actual.Pp.ShouldBe(session.Pp);
            actual.Metadata.ShouldBe(session.Metadata);
            actual.Authentication.ShouldBe(session.Authentication);
            actual.Compression.ShouldBe(session.Compression);
            actual.CompressionOptions.ShouldBe(session.CompressionOptions);
            actual.Encryption.ShouldBe(session.Encryption);
            actual.EncryptionOptions.ShouldBe(session.EncryptionOptions);
            actual.Reason.ShouldBe(session.Reason);
            actual.Scheme.ShouldBe(session.Scheme);
            actual.SchemeOptions.ShouldBe(session.SchemeOptions);
            actual.State.ShouldBe(session.State);
        }
    }
}