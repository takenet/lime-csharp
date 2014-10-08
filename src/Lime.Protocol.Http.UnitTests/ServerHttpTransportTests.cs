using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using Lime.Protocol.UnitTests;
using Lime.Protocol.Security;
using Lime.Protocol.Http.Storage;
using Moq;
using System.Threading;
using Shouldly;

namespace Lime.Protocol.Http.UnitTests
{
    /// <summary>
    /// Summary description for ServerHttpTransportTests
    /// </summary>
    [TestClass]
    public class ServerHttpTransportTests
    {

        public Identity Identity { get; set; }

        public Authentication Authentication { get; set; }

        public bool IsHttps { get; set; }

        public Mock<IEnvelopeStorage<Message>> MessageStorage { get; set; }

        public Mock<IEnvelopeStorage<Notification>> NotificationStorage { get; set; }

        public TimeSpan ExpirationInactivityInternal { get; set; }

        public CancellationToken CancellationToken { get; set; }

        Lazy<ServerHttpTransport> Target { get; set; }

        public Message Message { get; set; }

        public Notification Notification { get; set; }

        [TestInitialize]
        public void Arrange()
        {
            Identity = DataUtil.CreateIdentity();
            Authentication = DataUtil.CreateAuthentication(AuthenticationScheme.Plain);
            IsHttps = true;

            MessageStorage = new Mock<IEnvelopeStorage<Message>>();
            NotificationStorage = new Mock<IEnvelopeStorage<Notification>>();

            ExpirationInactivityInternal = TimeSpan.FromSeconds(10);

            CancellationToken = TimeSpan.FromSeconds(5).ToCancellationToken();

            Message = DataUtil.CreateMessage(DataUtil.CreateTextContent());

            Notification = DataUtil.CreateNotification(Event.Dispatched);
            Notification.Id = Message.Id;

            Target = new Lazy<ServerHttpTransport>(() =>
                new ServerHttpTransport(Identity, Authentication, IsHttps, MessageStorage.Object, NotificationStorage.Object, ExpirationInactivityInternal));
        }

        [TestMethod]
        public async Task SubmitAsync_ValidEnvelope_SendsToBuffer()
        {            
            // Arrange
            DateTimeOffset now = DateTimeOffset.UtcNow;

            // Act
            await Target.Value.SubmitAsync(Message, CancellationToken);
            var actual = await Target.Value.ReceiveAsync(CancellationToken);

            // Assert
            actual.ShouldBe(Message);
            Target.Value.Expiration.ShouldBeGreaterThanOrEqualTo(now.Add(ExpirationInactivityInternal));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task SubmitAsync_NullEnvelope_ThrowsArgumentNullException()
        {
            // Arrange            
            Envelope envelope = null;

            // Act
            await Target.Value.SubmitAsync(envelope, CancellationToken);
        }


        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task SubmitAsync_CompletedBuffer_ThrowsInvalidOperationException()
        {
            // Arrange            
            await Target.Value.CloseAsync(CancellationToken);

            // Act
            await Target.Value.SubmitAsync(Message, CancellationToken);
        }

        [TestMethod]
        public async Task ProcessMessageAsync_ValidMessage_ReturnsNotification()
        {
            // Act
            var notificationTask = Target.Value.ProcessMessageAsync(Message, CancellationToken);
            await Target.Value.SendAsync(Notification, CancellationToken);

            // Assert
            var actual = await notificationTask;
            actual.ShouldBe(Notification);

            // Check for the removal of the pending notification
            NotificationStorage.Verify(n => n.StoreEnvelopeAsync(It.IsAny<Identity>(), It.IsAny<Notification>()), Times.Never());
            await Target.Value.SendAsync(Notification, CancellationToken);
            NotificationStorage.Verify(n => n.StoreEnvelopeAsync(It.IsAny<Identity>(), It.IsAny<Notification>()), Times.Once());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task ProcessMessageAsync_NullMessage_ThrowsArgumentNullException()
        {
            // Arrange
            Message message = null;

            // Act
            await Target.Value.ProcessMessageAsync(message, CancellationToken);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task ProcessMessageAsync_FireAndForgetMessage_ThrowsArgumentException()
        {
            // Arrange
            Message.Id = Guid.Empty;

            // Act
            await Target.Value.ProcessMessageAsync(Message, CancellationToken);
        }

        [TestMethod]
        [ExpectedException(typeof(TaskCanceledException))]
        public async Task ProcessMessageAsync_NoNotification_ThrowsOperationCanceledException()
        {
            // Arrange
            CancellationToken = TimeSpan.FromSeconds(1).ToCancellationToken();

            // Act
            await Target.Value.ProcessMessageAsync(Message, CancellationToken);
        }

    }
}