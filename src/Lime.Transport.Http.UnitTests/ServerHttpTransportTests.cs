using System;
using System.Threading;
using System.Threading.Tasks;
using Lime.Messaging.Resources;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Security;
using Lime.Protocol.UnitTests;
using Lime.Transport.Http.Storage;
using NUnit.Framework;
using Moq;
using Shouldly;

namespace Lime.Transport.Http.UnitTests
{
    /// <summary>
    /// Summary description for ServerHttpTransportTests
    /// </summary>
    [TestFixture]
    public class ServerHttpTransportTests
    {

        public Identity Identity { get; set; }

        public Authentication Authentication { get; set; }

        public bool UseHttps { get; set; }

        public Mock<IEnvelopeStorage<Message>> MessageStorage { get; set; }

        public Mock<IEnvelopeStorage<Notification>> NotificationStorage { get; set; }

        public TimeSpan ExpirationInactivityInternal { get; set; }

        public CancellationToken CancellationToken { get; set; }

        Lazy<ServerHttpTransport> Target { get; set; }

        public Message TextMessage { get; set; }

        public Notification AcceptedNotification { get; set; }

        public Notification DispatchedNotification { get; set; }

        public Notification FailedNotification { get; set; }

        public Event WaitUntilEvent { get; set; }

        public Command PresenceRequestCommand { get; set; }

        public Command PresenceResponseCommand { get; set; }

        public Command PingRequestCommand { get; set; }

        public Session OptionsNegotiatingSession { get; set; }

        public Session ConfirmationNegotiatingSession { get; set; }

        public Session OptionsAuthenticatingSession { get; set; }

        public Session EstablishedSession { get; set; }

        public Session FinishedSession { get; set; }

        public Session FailedSession { get; set; }



        [SetUp]
        public void Arrange()
        {
            Identity = DataUtil.CreateIdentity();
            Authentication = DataUtil.CreateAuthentication(AuthenticationScheme.Plain);
            UseHttps = true;

            MessageStorage = new Mock<IEnvelopeStorage<Message>>();
            NotificationStorage = new Mock<IEnvelopeStorage<Notification>>();

            ExpirationInactivityInternal = TimeSpan.FromSeconds(10);

            CancellationToken = TimeSpan.FromSeconds(5).ToCancellationToken();

            TextMessage = DataUtil.CreateMessage(DataUtil.CreateTextContent());

            AcceptedNotification = DataUtil.CreateNotification(Event.Accepted);
            AcceptedNotification.Id = TextMessage.Id;
            AcceptedNotification.To = TextMessage.From;

            DispatchedNotification = DataUtil.CreateNotification(Event.Dispatched);
            DispatchedNotification.Id = TextMessage.Id;
            DispatchedNotification.To = TextMessage.From;

            FailedNotification = DataUtil.CreateNotification(Event.Failed);
            FailedNotification.Id = TextMessage.Id;
            FailedNotification.To = TextMessage.From;
            FailedNotification.Reason = DataUtil.CreateReason();

            WaitUntilEvent = DispatchedNotification.Event;

            PresenceRequestCommand = DataUtil.CreateCommand();
            PresenceRequestCommand.Uri = new LimeUri(UriTemplates.PRESENCE);            
            PresenceResponseCommand = DataUtil.CreateCommand(DataUtil.CreatePresence(), status: CommandStatus.Success);
            PresenceResponseCommand.Id = PresenceRequestCommand.Id;

            PingRequestCommand = DataUtil.CreateCommand();
            PingRequestCommand.Uri = new LimeUri(UriTemplates.PING);            

            OptionsNegotiatingSession = DataUtil.CreateSession(SessionState.Negotiating);
            OptionsNegotiatingSession.CompressionOptions = new[] { SessionCompression.None };
            OptionsNegotiatingSession.EncryptionOptions = new[] { SessionEncryption.None, SessionEncryption.TLS };            
            ConfirmationNegotiatingSession = DataUtil.CreateSession(SessionState.Negotiating);
            ConfirmationNegotiatingSession.Id = OptionsNegotiatingSession.Id;
            ConfirmationNegotiatingSession.Compression = SessionCompression.None;
            ConfirmationNegotiatingSession.Encryption = SessionEncryption.TLS;
            OptionsAuthenticatingSession = DataUtil.CreateSession(SessionState.Authenticating);
            OptionsAuthenticatingSession.Id = OptionsNegotiatingSession.Id;
            OptionsAuthenticatingSession.SchemeOptions = new[] { AuthenticationScheme.Plain };
            EstablishedSession = DataUtil.CreateSession(SessionState.Established);
            EstablishedSession.Id = OptionsNegotiatingSession.Id;
            FinishedSession = DataUtil.CreateSession(SessionState.Finished);
            FinishedSession.Id = OptionsNegotiatingSession.Id;
            FailedSession = DataUtil.CreateSession(SessionState.Failed);
            FailedSession.Id = OptionsNegotiatingSession.Id;
            FailedSession.Reason = DataUtil.CreateReason();


            Target = new Lazy<ServerHttpTransport>(() =>
                new ServerHttpTransport(Identity, Authentication, UseHttps, MessageStorage.Object, NotificationStorage.Object, ExpirationInactivityInternal));
        }

        [Test]
        public async Task SubmitAsync_ValidEnvelope_SendsToBuffer()
        {            
            // Arrange
            DateTimeOffset now = DateTimeOffset.UtcNow;

            // Act
            await Target.Value.SubmitAsync(TextMessage, CancellationToken);
            var actual = await Target.Value.ReceiveAsync(CancellationToken);

            // Assert
            actual.ShouldBe(TextMessage);
            Target.Value.Expiration.ShouldBeGreaterThanOrEqualTo(now.Add(ExpirationInactivityInternal));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task SubmitAsync_NullEnvelope_ThrowsArgumentNullException()
        {
            // Arrange            
            Envelope envelope = null;

            // Act
            await Target.Value.SubmitAsync(envelope, CancellationToken);
        }


        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task SubmitAsync_CompletedBuffer_ThrowsInvalidOperationException()
        {
            // Arrange            
            await Target.Value.CloseAsync(CancellationToken);

            // Act
            await Target.Value.SubmitAsync(TextMessage, CancellationToken);
        }

        [Test]
        public async Task ProcessMessageAsync_ValidMessage_ReturnsNotification()
        {
            // Arrange
            NotificationStorage
                .Setup(n => n.StoreEnvelopeAsync(It.IsAny<Identity>(), It.IsAny<Notification>()))
                .ReturnsAsync(true);
                    
            // Act
            var notificationTask = Target.Value.ProcessMessageAsync(TextMessage, WaitUntilEvent, CancellationToken);
            await Target.Value.SendAsync(AcceptedNotification, CancellationToken);
            await Target.Value.SendAsync(DispatchedNotification, CancellationToken);

            // Assert
            var actual = await notificationTask;
            actual.ShouldBe(DispatchedNotification);

            // Check for the removal of the pending notification
            NotificationStorage.Verify(n => n.StoreEnvelopeAsync(It.IsAny<Identity>(), It.IsAny<Notification>()), Times.Never());
            await Target.Value.SendAsync(DispatchedNotification, CancellationToken);
            NotificationStorage.Verify(n => n.StoreEnvelopeAsync(It.IsAny<Identity>(), It.IsAny<Notification>()), Times.Once());
        }

        [Test]
        public async Task ProcessMessageAsync_ValidMessageFailedNotification_ReturnsNotification()
        {
            // Arrange
            NotificationStorage
                .Setup(n => n.StoreEnvelopeAsync(It.IsAny<Identity>(), It.IsAny<Notification>()))
                .ReturnsAsync(true);

            // Act
            var notificationTask = Target.Value.ProcessMessageAsync(TextMessage, WaitUntilEvent, CancellationToken);
            await Target.Value.SendAsync(AcceptedNotification, CancellationToken);
            await Target.Value.SendAsync(FailedNotification, CancellationToken);

            // Assert
            var actual = await notificationTask;
            actual.ShouldBe(FailedNotification);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task ProcessMessageAsync_NullMessage_ThrowsArgumentNullException()
        {
            // Arrange
            Message message = null;

            // Act
            await Target.Value.ProcessMessageAsync(message, WaitUntilEvent, CancellationToken);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public async Task ProcessMessageAsync_FireAndForgetMessage_ThrowsArgumentException()
        {
            // Arrange
            TextMessage.Id = Guid.Empty;

            // Act
            await Target.Value.ProcessMessageAsync(TextMessage, WaitUntilEvent, CancellationToken);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task ProcessMessageAsync_DuplicateMessageId_ThrowsInvalidOperationException()
        {
            // Act
            Target.Value.ProcessMessageAsync(TextMessage, WaitUntilEvent, CancellationToken);
            await Target.Value.ProcessMessageAsync(TextMessage, WaitUntilEvent, CancellationToken);
        }

        [Test]
        [ExpectedException(typeof(TaskCanceledException))]
        public async Task ProcessMessageAsync_NoNotification_ThrowsOperationCanceledException()
        {
            // Arrange
            CancellationToken = TimeSpan.FromSeconds(1).ToCancellationToken();

            // Act
            await Target.Value.ProcessMessageAsync(TextMessage, WaitUntilEvent, CancellationToken);
        }

        [Test]
        public async Task ProcessCommandAsync_ValidCommand_ReturnsResponse()
        {
            // Arrange
            NotificationStorage
                .Setup(n => n.StoreEnvelopeAsync(It.IsAny<Identity>(), It.IsAny<Notification>()))
                .ReturnsAsync(true);

            // Act
            var commandTask = Target.Value.ProcessCommandAsync(PresenceRequestCommand, CancellationToken);
            await Target.Value.SendAsync(PresenceResponseCommand, CancellationToken);

            // Assert
            var actual = await commandTask;
            actual.ShouldBe(PresenceResponseCommand);

            // Check for the removal of the pending notification
            NotificationStorage.Verify(n => n.StoreEnvelopeAsync(It.IsAny<Identity>(), It.IsAny<Notification>()), Times.Never());
            await Target.Value.SendAsync(DispatchedNotification, CancellationToken);
            NotificationStorage.Verify(n => n.StoreEnvelopeAsync(It.IsAny<Identity>(), It.IsAny<Notification>()), Times.Once());
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task ProcessCommandAsync_NullCommand_ThrowsArgumentNullException()
        {
            // Arrange
            Command command = null;

            // Act
            await Target.Value.ProcessCommandAsync(command, CancellationToken);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public async Task ProcessCommandAsync_EmptyIdCommand_ThrowsArgumentException()
        {
            // Arrange
            PresenceRequestCommand.Id = Guid.Empty;

            // Act
            await Target.Value.ProcessCommandAsync(PresenceRequestCommand, CancellationToken);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task ProcessCommandAsync_DuplicateCommandId_ThrowsInvalidOperationException()
        {
            // Act
            Target.Value.ProcessCommandAsync(PresenceRequestCommand, CancellationToken);
            await Target.Value.ProcessCommandAsync(PresenceRequestCommand, CancellationToken);
        }

        [Test]
        [ExpectedException(typeof(TaskCanceledException))]
        public async Task ProcessCommandAsync_NoResponse_ThrowsOperationCanceledException()
        {
            // Arrange
            CancellationToken = TimeSpan.FromSeconds(1).ToCancellationToken();

            // Act
            await Target.Value.ProcessCommandAsync(PresenceRequestCommand, CancellationToken);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public async Task ProcessCommandAsync_ResponseCommand_ThrowsArgumentException()
        {
            // Arrange
            PresenceRequestCommand.Status = CommandStatus.Success;

            // Act
            await Target.Value.ProcessCommandAsync(PresenceRequestCommand, CancellationToken);
        }

        [Test]
        public async Task GetSessionAsync_FullSessionNegotiation_ReturnsSession()
        {
            // Act
            var authenticationTask = Target.Value.GetSessionAsync(CancellationToken);
            await Target.Value.SendAsync(OptionsNegotiatingSession, CancellationToken);
            await Target.Value.SendAsync(OptionsAuthenticatingSession, CancellationToken);
            await Target.Value.SendAsync(EstablishedSession, CancellationToken);

            var actual = await authenticationTask;

            // Assert
            actual.ShouldBe(EstablishedSession);
            var envelope1 = await Target.Value.ReceiveAsync(CancellationToken);
            var session1 = envelope1.ShouldBeOfType<Session>();
            session1.State.ShouldBe(SessionState.Negotiating);
            var envelope2 = await Target.Value.ReceiveAsync(CancellationToken);
            var session2 = envelope2.ShouldBeOfType<Session>();
            session2.State.ShouldBe(SessionState.Authenticating);            
        }

        [Test]
        public async Task GetSessionAsync_AlreadyCompleted_ReturnsSession()
        {
            // Act
            var authenticationTask = Target.Value.GetSessionAsync(CancellationToken);
            await Target.Value.SendAsync(EstablishedSession, CancellationToken);
            await authenticationTask;
            var actual = await Target.Value.GetSessionAsync(CancellationToken);
           
            // Assert
            actual.ShouldBe(EstablishedSession);
        }

        [Test]
        public async Task FinishAsync_EstablishedSession_SendsSessionFinishing()
        {
            // Arrange
            await Target.Value.SendAsync(EstablishedSession, CancellationToken);

            // Act                                    
            var finishTask = Target.Value.FinishAsync(CancellationToken);
            await Target.Value.SendAsync(FinishedSession, CancellationToken);

            await finishTask;            

            // Assert
            var envelope1 = await Target.Value.ReceiveAsync(CancellationToken);
            var session1 = envelope1.ShouldBeOfType<Session>();
            session1.State.ShouldBe(SessionState.Finishing);
        }

        [Test]
        public async Task FinishAsync_EstablishedSessionFinishingFailed_ThrowsLimeException()
        {
            // Arrange
            await Target.Value.SendAsync(EstablishedSession, CancellationToken);

            // Act                                    
            try
            {
                var finishTask = Target.Value.FinishAsync(CancellationToken);
                await Target.Value.SendAsync(FailedSession, CancellationToken);
                await finishTask;

            }
            catch (LimeException ex)
            {
                ex.Reason.Code.ShouldBe(FailedSession.Reason.Code);
                ex.Reason.Description.ShouldBe(FailedSession.Reason.Description);
            }
            // Assert
            var envelope1 = await Target.Value.ReceiveAsync(CancellationToken);
            var session1 = envelope1.ShouldBeOfType<Session>();
            session1.State.ShouldBe(SessionState.Finishing);
        }

        [Test]
        public async Task FinishAsync_EstablishedSessionFinishingFailedNoReason_ThrowsLimeException()
        {
            // Arrange
            await Target.Value.SendAsync(EstablishedSession, CancellationToken);
            FailedSession.Reason = null;

            // Act                                    
            try
            {
                var finishTask = Target.Value.FinishAsync(CancellationToken);
                await Target.Value.SendAsync(FailedSession, CancellationToken);
                await finishTask;

            }
            catch (LimeException ex)
            {
                ex.Reason.Code.ShouldBe(ReasonCodes.SESSION_ERROR);
            }
            // Assert
            var envelope1 = await Target.Value.ReceiveAsync(CancellationToken);
            var session1 = envelope1.ShouldBeOfType<Session>();
            session1.State.ShouldBe(SessionState.Finishing);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task FinishAsync_FailedSession_ThrowsInvalidOperationException()
        {
            // Arrange
            await Target.Value.SendAsync(FailedSession, CancellationToken);

            // Act                                    
            await Target.Value.FinishAsync(CancellationToken);
        }

        [Test]
        public void GetSupportedCompression_ReturnsNone()
        {
            // Assert
            Target.Value.Compression.ShouldBe(SessionCompression.None);
        }

        [Test]
        public void GetSupportedEncryption_IsHttpsTrue_ReturnsTLS()
        {
            // Act
            UseHttps = true;

            // Assert
            Target.Value.Encryption.ShouldBe(SessionEncryption.TLS);
        }

        [Test]
        public void GetSupportedEncryption_IsHttpsFalse_ReturnsNone()
        {
            // Act
            UseHttps = false;

            // Assert
            Target.Value.Encryption.ShouldBe(SessionEncryption.None);
        }

        [Test]
        public async Task SendAsync_Message_SendsToStorage()
        {
            // Arrange
            MessageStorage
                .Setup(m => m.StoreEnvelopeAsync(TextMessage.To.ToIdentity(), TextMessage))
                .ReturnsAsync(true)
                .Verifiable();

            // Act
            await Target.Value.SendAsync(TextMessage, CancellationToken);

            // Assert
            MessageStorage.Verify();
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task SendAsync_MessageStorageFailed_ThrowsInvalidOperationException()
        {
            // Arrange
            MessageStorage
                .Setup(m => m.StoreEnvelopeAsync(TextMessage.To.ToIdentity(), TextMessage))
                .ReturnsAsync(false)
                .Verifiable();

            // Act
            await Target.Value.SendAsync(TextMessage, CancellationToken);
        }

        [Test]
        public async Task SendAsync_OptionsNegotiatingSession_SendsNegotiatingResponseToBuffer()
        {            
            // Act
            await Target.Value.SendAsync(OptionsNegotiatingSession, CancellationToken);

            // Assert
            var envelope = await Target.Value.ReceiveAsync(CancellationToken);
            var negotiatingResponseSession = envelope.ShouldBeOfType<Session>();
            negotiatingResponseSession.Id.ShouldBe(OptionsNegotiatingSession.Id);
            negotiatingResponseSession.State.ShouldBe(SessionState.Negotiating);
            negotiatingResponseSession.Compression.ShouldBe(Target.Value.Compression);
            negotiatingResponseSession.Encryption.ShouldBe(Target.Value.Encryption);
        }

        [Test]
        public async Task SendAsync_ConfirmationNegotiatingSession_DoesNothing()
        {
            // Act
            await Target.Value.SendAsync(ConfirmationNegotiatingSession, CancellationToken);

            // Assert
            var envelopeTask = Target.Value.ReceiveAsync(CancellationToken);
            await Task.Delay(100);
            envelopeTask.IsCompleted.ShouldBe(false);
        }

        [Test]
        public async Task SendAsync_OptionsAuthenticatingSession_SendsAuthenticationResponseToBuffer()
        {
            // Act
            await Target.Value.SendAsync(OptionsAuthenticatingSession, CancellationToken);

            // Assert
            var envelope = await Target.Value.ReceiveAsync(CancellationToken);
            var authenticationResponseSession = envelope.ShouldBeOfType<Session>();
            authenticationResponseSession.Id.ShouldBe(OptionsAuthenticatingSession.Id);
            authenticationResponseSession.State.ShouldBe(SessionState.Authenticating);
            authenticationResponseSession.Scheme.ShouldBe(AuthenticationScheme.Plain);
            authenticationResponseSession.Authentication.ShouldBe(Authentication);
            authenticationResponseSession.From.ShouldNotBe(null);
            authenticationResponseSession.From.ToIdentity().ShouldBe(Identity);
        }

        [Test]
        public async Task SendAsync_UnsupportedAuthenticationScheme_SendsFinishingToBuffer()
        {
            // Arrange
            OptionsAuthenticatingSession.SchemeOptions = new[] { AuthenticationScheme.Guest };

            // Act
            await Target.Value.SendAsync(OptionsAuthenticatingSession, CancellationToken);

            // Assert
            var envelope = await Target.Value.ReceiveAsync(CancellationToken);
            var authenticationResponseSession = envelope.ShouldBeOfType<Session>();
            authenticationResponseSession.Id.ShouldBe(OptionsAuthenticatingSession.Id);
            authenticationResponseSession.State.ShouldBe(SessionState.Finishing);            
        }

        [Test]
        public async Task SendAsync_EstablishedSession_CompletesAuthenticationTask()
        {            
            // Arrange
            var authenticationTask = Target.Value.GetSessionAsync(CancellationToken);
            authenticationTask.IsCompleted.ShouldBe(false);

            // Act
            await Target.Value.SendAsync(EstablishedSession, CancellationToken);

            // Assert
            var actual = await authenticationTask;
            actual.ShouldBe(EstablishedSession);
        }

        [Test]
        public async Task SendAsync_FailedSession_CompletesAuthenticationTask()
        {
            // Arrange
            var authenticationTask = Target.Value.GetSessionAsync(CancellationToken);
            authenticationTask.IsCompleted.ShouldBe(false);

            // Act
            await Target.Value.SendAsync(FailedSession, CancellationToken);

            // Assert
            var actual = await authenticationTask;
            actual.ShouldBe(FailedSession);
        }

        [Test]
        public async Task SendAsync_AlreadyEstablishedFinishedSession_CompletesFinishTask()
        {
            // Arrange
            var finishTask = Target.Value.FinishAsync(CancellationToken);
            await Target.Value.SendAsync(EstablishedSession, CancellationToken);
            finishTask.IsCompleted.ShouldBe(false);

            // Act
            await Target.Value.SendAsync(FinishedSession, CancellationToken);

            // Assert
            await finishTask;
        }

        [Test]
        public async Task SendAsync_AlreadyEstablishedFailedSession_CompletesFinishTask()
        {
            // Arrange
            var finishTask = Target.Value.FinishAsync(CancellationToken);
            await Target.Value.SendAsync(EstablishedSession, CancellationToken);
            finishTask.IsCompleted.ShouldBe(false);

            // Act
            await Target.Value.SendAsync(FailedSession, CancellationToken);

            // Assert
            try
            {
                await finishTask;
            }
            catch (LimeException ex)
            {
                ex.Reason.Code.ShouldBe(FailedSession.Reason.Code);
                ex.Reason.Description.ShouldBe(FailedSession.Reason.Description);
            }
        }

        [Test]
        public async Task SendAsync_FailedNotificationForPendingMessage_CompletesTask()
        {
            // Arrange            
            var processMessageTask = Target.Value.ProcessMessageAsync(TextMessage, WaitUntilEvent, CancellationToken);
            processMessageTask.IsCompleted.ShouldBe(false);

            // Act
            await Target.Value.SendAsync(FailedNotification, CancellationToken);

            // Assert
            var actual = await processMessageTask;
            actual.ShouldBe(FailedNotification);
        }

        [Test]
        public async Task SendAsync_FailedNotification_SendsToStorage()
        {
            // Arrange
            NotificationStorage
                .Setup(m => m.StoreEnvelopeAsync(FailedNotification.To.ToIdentity(), FailedNotification))
                .ReturnsAsync(true)
                .Verifiable();
            
            // Act
            await Target.Value.SendAsync(FailedNotification, CancellationToken);

            // Assert
            NotificationStorage.Verify();            
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task SendAsync_NotificationStorageFailed_ThrowsInvalidOperationException()
        {
            // Arrange
            NotificationStorage
                .Setup(m => m.StoreEnvelopeAsync(DispatchedNotification.To.ToIdentity(), DispatchedNotification))
                .ReturnsAsync(false);

            // Act
            await Target.Value.SendAsync(DispatchedNotification, CancellationToken);
        }

        [Test]
        public async Task SendAsync_OrderedNotifications_UpdatesOnStorage()
        {
            // Arrange
            NotificationStorage
                .Setup(m => m.StoreEnvelopeAsync(DispatchedNotification.To.ToIdentity(), DispatchedNotification))
                .ReturnsAsync(true)
                .Verifiable();

            NotificationStorage
                .Setup(m => m.StoreEnvelopeAsync(FailedNotification.To.ToIdentity(), FailedNotification))
                .ReturnsAsync(true)
                .Verifiable();

            NotificationStorage
                .Setup(m => m.DeleteEnvelopeAsync(FailedNotification.To.ToIdentity(), DispatchedNotification.Id))
                .ReturnsAsync(true)
                .Verifiable();

            NotificationStorage
                .SetupSequence(m => m.GetEnvelopeAsync(DispatchedNotification.To.ToIdentity(), DispatchedNotification.Id))
                .Returns(Task.FromResult<Notification>(null))
                .Returns(Task.FromResult<Notification>(DispatchedNotification));                                   

            // Act
            await Target.Value.SendAsync(DispatchedNotification, CancellationToken);
            await Target.Value.SendAsync(FailedNotification, CancellationToken);

            // Assert
            NotificationStorage.Verify();
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task SendAsync_OrderedNotificationsUpdateFailed_ThrowsInvalidOperationException()
        {
            // Arrange
            NotificationStorage
                .Setup(m => m.StoreEnvelopeAsync(DispatchedNotification.To.ToIdentity(), DispatchedNotification))
                .ReturnsAsync(true)
                .Verifiable();

            NotificationStorage
                .Setup(m => m.StoreEnvelopeAsync(FailedNotification.To.ToIdentity(), FailedNotification))
                .ReturnsAsync(false)
                .Verifiable();

            NotificationStorage
                .Setup(m => m.DeleteEnvelopeAsync(FailedNotification.To.ToIdentity(), DispatchedNotification.Id))
                .ReturnsAsync(true)
                .Verifiable();

            NotificationStorage
                .SetupSequence(m => m.GetEnvelopeAsync(DispatchedNotification.To.ToIdentity(), DispatchedNotification.Id))
                .Returns(Task.FromResult<Notification>(null))
                .Returns(Task.FromResult<Notification>(DispatchedNotification));

            // Act
            await Target.Value.SendAsync(DispatchedNotification, CancellationToken);
            await Target.Value.SendAsync(FailedNotification, CancellationToken);            
        }

        [Test]
        public async Task SendAsync_UnorderedNotifications_KeepsTheLastestOnStorage()
        {
            // Arrange
            NotificationStorage
                .Setup(m => m.StoreEnvelopeAsync(DispatchedNotification.To.ToIdentity(), DispatchedNotification))
                .ReturnsAsync(true)
                .Verifiable();
                            
            NotificationStorage
                .SetupSequence(m => m.GetEnvelopeAsync(DispatchedNotification.To.ToIdentity(), DispatchedNotification.Id))
                .Returns(Task.FromResult<Notification>(null))
                .Returns(Task.FromResult<Notification>(DispatchedNotification));

            // Act
            await Target.Value.SendAsync(DispatchedNotification, CancellationToken);
            await Target.Value.SendAsync(AcceptedNotification, CancellationToken);

            // Assert
            NotificationStorage.Verify();
            NotificationStorage.Verify(m => m.StoreEnvelopeAsync(AcceptedNotification.To.ToIdentity(), AcceptedNotification), Times.Never());
            NotificationStorage.Verify(m => m.DeleteEnvelopeAsync(FailedNotification.To.ToIdentity(), DispatchedNotification.Id), Times.Never());               
        }

        [Test]
        public async Task SendAsync_UnorderedNotificationsWithFailed_KeepsTheLastestOnStorage()
        {
            // Arrange
            NotificationStorage
                .Setup(m => m.StoreEnvelopeAsync(FailedNotification.To.ToIdentity(), FailedNotification))
                .ReturnsAsync(true)
                .Verifiable();

            NotificationStorage
                .SetupSequence(m => m.GetEnvelopeAsync(FailedNotification.To.ToIdentity(), FailedNotification.Id))
                .Returns(Task.FromResult<Notification>(null))
                .Returns(Task.FromResult<Notification>(FailedNotification));

            // Act
            await Target.Value.SendAsync(FailedNotification, CancellationToken);
            await Target.Value.SendAsync(AcceptedNotification, CancellationToken);

            // Assert
            NotificationStorage.Verify();
            NotificationStorage.Verify(m => m.StoreEnvelopeAsync(AcceptedNotification.To.ToIdentity(), AcceptedNotification), Times.Never());
            NotificationStorage.Verify(m => m.DeleteEnvelopeAsync(FailedNotification.To.ToIdentity(), FailedNotification.Id), Times.Never());
        }

        [Test]
        public async Task SendAsync_ResponseCommandWithPendingRequest_CompletesTask()
        {
            // Arrange
            var requestCommandTask = Target.Value.ProcessCommandAsync(PresenceRequestCommand, CancellationToken);
            requestCommandTask.IsCompleted.ShouldBe(false);

            // Act
            await Target.Value.SendAsync(PresenceResponseCommand, CancellationToken);

            // Assert
            var actual = await requestCommandTask;
            actual.ShouldBe(PresenceResponseCommand);
        }

        [Test]
        public async Task SendAsync_ResponseCommand_DoesNothing()
        {
            // Act
            await Target.Value.SendAsync(PresenceResponseCommand, CancellationToken);

            // Assert
            var envelopeTask = Target.Value.ReceiveAsync(CancellationToken);
            await Task.Delay(100);
            envelopeTask.IsCompleted.ShouldBe(false);
        }

        [Test]
        public async Task SendAsync_PingRequestCommand_SendsSuccessResponseToBuffer()
        {
            // Act
            await Target.Value.SendAsync(PingRequestCommand, CancellationToken);

            // Assert
            var envelope = await Target.Value.ReceiveAsync(CancellationToken);
            var responseCommand = envelope.ShouldBeOfType<Command>();
            responseCommand.Id.ShouldBe(PingRequestCommand.Id);
            responseCommand.Status.ShouldBe(CommandStatus.Success);
        }

        [Test]
        public async Task SendAsync_RequestCommandWithPendingRequest_SendsNotSupportedResponseToBuffer()
        {
            // Act
            await Target.Value.SendAsync(PresenceRequestCommand, CancellationToken);

            // Assert
            var envelope = await Target.Value.ReceiveAsync(CancellationToken);
            var responseCommand = envelope.ShouldBeOfType<Command>();
            responseCommand.Id.ShouldBe(PresenceRequestCommand.Id);
            responseCommand.Status.ShouldBe(CommandStatus.Failure);
            responseCommand.Reason.ShouldNotBe(null);
            responseCommand.Reason.Code.ShouldBe(ReasonCodes.COMMAND_RESOURCE_NOT_SUPPORTED);
        }
        
        [Test]
        public async Task OpenAsync_SendsNewSessionToBuffer()
        {
            // Act
            await Target.Value.OpenAsync(It.IsAny<Uri>(), CancellationToken);

            // Assert
            var envelope = await Target.Value.ReceiveAsync(CancellationToken);
            var session = envelope.ShouldBeOfType<Session>();
            session.Id.ShouldBe(Guid.Empty);
            session.State.ShouldBe(SessionState.New);
        }

        [Test]
        public async Task CloseAsync_ClosesInputBufferAndCancelsPendingTasks()
        {
            // Arrange
            var sessionTask = Target.Value.GetSessionAsync(CancellationToken);
            var commandTask = Target.Value.ProcessCommandAsync(PresenceRequestCommand, CancellationToken);
            var notificationTask = Target.Value.ProcessMessageAsync(TextMessage, WaitUntilEvent, CancellationToken);

            // Act
            await Target.Value.CloseAsync(CancellationToken);

            // Assert
            try
            {
                await Target.Value.SubmitAsync(TextMessage, CancellationToken);
            }
            catch (InvalidOperationException) {}

            sessionTask.IsCanceled.ShouldBe(true);
            commandTask.IsCanceled.ShouldBe(true);
            notificationTask.IsCanceled.ShouldBe(true);
        }
        
    }
}