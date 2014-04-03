using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Lime.Protocol.Serialization;
using Lime.Protocol.Contents;
using System.Collections.Generic;
using Lime.Protocol.Security;
using Lime.Protocol.Resources;

namespace Lime.Protocol.UnitTests.Serialization
{
    [TestClass]
    public class JsonNetSerializerTests
    {
        public JsonNetSerializer GetTarget()
        {
            return new JsonNetSerializer();
        }

        #region Serialize

        [TestMethod]
        [TestCategory("Serialize")]
        public void Serialize_CapabilityRequestCommand_ReturnsValidJsonString()
        {
            var target = GetTarget();

            var resource = DataUtil.CreateCapability();
            var command = DataUtil.CreateCommand(resource);
            command.Pp = DataUtil.CreateNode();

            var metadataKey1 = "randomString1";
            var metadataValue1 = DataUtil.CreateRandomString(50);
            var metadataKey2 = "randomString2";
            var metadataValue2 = DataUtil.CreateRandomString(50);
            command.Metadata = new Dictionary<string, string>();
            command.Metadata.Add(metadataKey1, metadataValue1);
            command.Metadata.Add(metadataKey2, metadataValue2);

            var resultString = target.Serialize(command);

            Assert.IsTrue(resultString.HasValidJsonStackedBrackets());
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.ID_KEY, command.Id));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.FROM_KEY, command.From));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.PP_KEY, command.Pp));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.TO_KEY, command.To));
            Assert.IsTrue(resultString.ContainsJsonProperty(Command.METHOD_KEY, command.Method));
            Assert.IsTrue(resultString.ContainsJsonProperty(Command.TYPE_KEY, command.Resource.GetMediaType()));
            Assert.IsTrue(resultString.ContainsJsonKey(Command.RESOURCE_KEY));
            Assert.IsTrue(resultString.ContainsJsonProperty(Command.METHOD_KEY, command.Method));
            Assert.IsTrue(resultString.ContainsJsonProperty(metadataKey1, metadataValue1));
            Assert.IsTrue(resultString.ContainsJsonProperty(metadataKey2, metadataValue2));

            Assert.IsTrue(resultString.ContainsJsonProperty(Capability.CONTENT_TYPES_KEY, resource.ContentTypes));
            Assert.IsTrue(resultString.ContainsJsonProperty(Capability.RESOURCE_TYPES_KEY, resource.ResourceTypes));


            Assert.IsFalse(resultString.ContainsJsonKey(Command.STATUS_KEY));
            Assert.IsFalse(resultString.ContainsJsonKey(Command.REASON_KEY));
        }

        [TestMethod]
        [TestCategory("Serialize")]
        public void Serialize_FailurePingResponseCommand_ReturnsValidJsonString()
        {
            var target = GetTarget();

            var command = DataUtil.CreateCommand(status: CommandStatus.Failure);
            command.Reason = DataUtil.CreateReason();

            var resultString = target.Serialize(command);

            Assert.IsTrue(resultString.HasValidJsonStackedBrackets());
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.FROM_KEY, command.From));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.ID_KEY, command.Id));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.TO_KEY, command.To));
            Assert.IsTrue(resultString.ContainsJsonProperty(Command.METHOD_KEY, command.Method));
            Assert.IsTrue(resultString.ContainsJsonProperty(Command.STATUS_KEY, command.Status));
            Assert.IsTrue(resultString.ContainsJsonKey(Command.REASON_KEY));
            Assert.IsTrue(resultString.ContainsJsonProperty(Reason.CODE_KEY, command.Reason.Code));
            Assert.IsTrue(resultString.ContainsJsonProperty(Reason.DESCRIPTION_KEY, command.Reason.Description));

            Assert.IsFalse(resultString.ContainsJsonKey(Envelope.PP_KEY));
            Assert.IsFalse(resultString.ContainsJsonKey(Envelope.METADATA_KEY));
            Assert.IsFalse(resultString.ContainsJsonKey(Command.RESOURCE_KEY));
        }

        [TestMethod]
        [TestCategory("Serialize")]
        public void Serialize_TextMessage_ReturnsValidJsonString()
        {
            var target = GetTarget();

            var content = DataUtil.CreateTextContent();
            var message = DataUtil.CreateMessage(content);
            message.Pp = DataUtil.CreateNode();

            var metadataKey1 = "randomString1";
            var metadataValue1 = DataUtil.CreateRandomString(50);
            var metadataKey2 = "randomString2";
            var metadataValue2 = DataUtil.CreateRandomString(50);
            message.Metadata = new Dictionary<string, string>();
            message.Metadata.Add(metadataKey1, metadataValue1);
            message.Metadata.Add(metadataKey2, metadataValue2);

            var resultString = target.Serialize(message);

            Assert.IsTrue(resultString.HasValidJsonStackedBrackets());
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.ID_KEY, message.Id));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.FROM_KEY, message.From));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.PP_KEY, message.Pp));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.TO_KEY, message.To));
            Assert.IsTrue(resultString.ContainsJsonProperty(Message.TYPE_KEY, message.Content.GetMediaType()));
            Assert.IsTrue(resultString.ContainsJsonKey(Message.CONTENT_KEY));
            Assert.IsTrue(resultString.ContainsJsonProperty(TextContent.TEXT_KEY, content.Text));
            Assert.IsTrue(resultString.ContainsJsonProperty(metadataKey1, metadataValue1));
            Assert.IsTrue(resultString.ContainsJsonProperty(metadataKey2, metadataValue2));
        }

        [TestMethod]
        [TestCategory("Serialize")]
        public void Serialize_FireAndForgetTextMessage_ReturnsValidJsonString()
        {
            var target = GetTarget();

            var content = DataUtil.CreateTextContent();
            var message = DataUtil.CreateMessage(content);
            message.Id = null;

            var resultString = target.Serialize(message);

            Assert.IsTrue(resultString.HasValidJsonStackedBrackets());

            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.FROM_KEY, message.From));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.TO_KEY, message.To));
            Assert.IsTrue(resultString.ContainsJsonProperty(Message.TYPE_KEY, message.Content.GetMediaType()));
            Assert.IsTrue(resultString.ContainsJsonKey(Message.CONTENT_KEY));
            Assert.IsTrue(resultString.ContainsJsonProperty(TextContent.TEXT_KEY, content.Text));

            Assert.IsFalse(resultString.ContainsJsonKey(Envelope.ID_KEY));
            Assert.IsFalse(resultString.ContainsJsonKey(Envelope.PP_KEY));
            Assert.IsFalse(resultString.ContainsJsonKey(Envelope.METADATA_KEY));
        }

        [TestMethod]
        [TestCategory("Serialize")]
        public void Serialize_ReceivedNotification_ReturnsValidJsonString()
        {
            var target = GetTarget();

            var notification = DataUtil.CreateNotification(Event.Received);
            notification.Id = Guid.NewGuid();
            notification.Pp = DataUtil.CreateNode();

            var metadataKey1 = "randomString1";
            var metadataValue1 = DataUtil.CreateRandomString(50);
            var metadataKey2 = "randomString2";
            var metadataValue2 = DataUtil.CreateRandomString(50);
            notification.Metadata = new Dictionary<string, string>();
            notification.Metadata.Add(metadataKey1, metadataValue1);
            notification.Metadata.Add(metadataKey2, metadataValue2);

            var resultString = target.Serialize(notification);

            Assert.IsTrue(resultString.HasValidJsonStackedBrackets());
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.ID_KEY, notification.Id));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.FROM_KEY, notification.From));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.PP_KEY, notification.Pp));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.TO_KEY, notification.To));
            Assert.IsTrue(resultString.ContainsJsonProperty(Notification.EVENT_KEY, notification.Event));
            Assert.IsTrue(resultString.ContainsJsonProperty(metadataKey1, metadataValue1));
            Assert.IsTrue(resultString.ContainsJsonProperty(metadataKey2, metadataValue2));

            Assert.IsFalse(resultString.ContainsJsonKey(Notification.REASON_KEY));
        }

        [TestMethod]
        [TestCategory("Serialize")]
        public void Serialize_FailedNotification_ReturnsValidJsonString()
        {
            var target = GetTarget();

            var notification = DataUtil.CreateNotification(Event.Failed);
            notification.Id = Guid.NewGuid();
            notification.Reason = DataUtil.CreateReason();

            var resultString = target.Serialize(notification);

            Assert.IsTrue(resultString.HasValidJsonStackedBrackets());
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.ID_KEY, notification.Id));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.FROM_KEY, notification.From));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.TO_KEY, notification.To));
            Assert.IsTrue(resultString.ContainsJsonProperty(Notification.EVENT_KEY, notification.Event));
            Assert.IsTrue(resultString.ContainsJsonProperty(Reason.CODE_KEY, notification.Reason.Code));
            Assert.IsTrue(resultString.ContainsJsonProperty(Reason.DESCRIPTION_KEY, notification.Reason.Description));

            Assert.IsFalse(resultString.ContainsJsonKey(Envelope.PP_KEY));
            Assert.IsFalse(resultString.ContainsJsonKey(Envelope.METADATA_KEY));
        }

        [TestMethod]
        [TestCategory("Serialize")]
        public void Serialize_AuthenticatingSession_ReturnsValidJsonString()
        {
            var target = GetTarget();

            var session = DataUtil.CreateSession();
            var plainAuthentication = DataUtil.CreatePlainAuthentication();
            session.Authentication = plainAuthentication;
            session.State = SessionState.Authenticating;

            var metadataKey1 = "randomString1";
            var metadataValue1 = DataUtil.CreateRandomString(50);
            var metadataKey2 = "randomString2";
            var metadataValue2 = DataUtil.CreateRandomString(50);
            session.Metadata = new Dictionary<string, string>();
            session.Metadata.Add(metadataKey1, metadataValue1);
            session.Metadata.Add(metadataKey2, metadataValue2);

            var resultString = target.Serialize(session);

            Assert.IsTrue(resultString.HasValidJsonStackedBrackets());
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.ID_KEY, session.Id));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.FROM_KEY, session.From));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.TO_KEY, session.To));
            Assert.IsTrue(resultString.ContainsJsonProperty(Session.STATE_KEY, session.State));
            Assert.IsTrue(resultString.ContainsJsonProperty(metadataKey1, metadataValue1));
            Assert.IsTrue(resultString.ContainsJsonProperty(metadataKey2, metadataValue2));
            Assert.IsTrue(resultString.ContainsJsonKey(Session.AUTHENTICATION_KEY));
            Assert.IsTrue(resultString.ContainsJsonProperty(PlainAuthentication.PASSWORD_KEY, plainAuthentication.Password));

            Assert.IsFalse(resultString.ContainsJsonKey(Envelope.PP_KEY));
            Assert.IsFalse(resultString.ContainsJsonKey(Session.REASON_KEY));
        }

        [TestMethod]
        [TestCategory("Serialize")]
        public void Serialize_FailedSession_ReturnsValidJsonString()
        {
            var target = GetTarget();

            var session = DataUtil.CreateSession();
            session.State = SessionState.Failed;
            session.Reason = DataUtil.CreateReason();

            var resultString = target.Serialize(session);

            Assert.IsTrue(resultString.HasValidJsonStackedBrackets());
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.ID_KEY, session.Id));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.FROM_KEY, session.From));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.TO_KEY, session.To));
            Assert.IsTrue(resultString.ContainsJsonProperty(Session.STATE_KEY, session.State));
            Assert.IsTrue(resultString.ContainsJsonKey(Session.REASON_KEY));
            Assert.IsTrue(resultString.ContainsJsonProperty(Reason.CODE_KEY, session.Reason.Code));
            Assert.IsTrue(resultString.ContainsJsonProperty(Reason.DESCRIPTION_KEY, session.Reason.Description));

            Assert.IsFalse(resultString.ContainsJsonKey(Envelope.PP_KEY));
            Assert.IsFalse(resultString.ContainsJsonKey(Envelope.METADATA_KEY));
            Assert.IsFalse(resultString.ContainsJsonKey(Session.AUTHENTICATION_KEY));
        }

        #endregion

        [TestMethod]
        [TestCategory("Deserialize")]
        public void Deserialize_CapabilityRequestCommand_ReturnsValidInstance()
        {
            var target = GetTarget();

            string json = "{\"type\":\"application/vnd.lime.capability+json\",\"resource\":{\"contentTypes\":[\"application/hf8vc1srhz+json\",\"application/r28ymkwa9g+json\",\"application/zcbre2qp85+json\"],\"resourceTypes\":[\"application/wbu3p657d0+json\",\"application/3hobug1nvf+json\",\"application/a50d5fdqqk+json\"]},\"method\":\"get\",\"id\":\"c881a050-75ca-4a48-b340-af31a0be2d66\",\"from\":\"kdsldphf@limeprotocol.org/home\",\"pp\":\"xa78ad32@limeprotocol.org/home\",\"to\":\"iqpz6imr@limeprotocol.org/home\",\"metadata\":{\"randomString1\":\"50lr8k2oq3qzyd7e8wef0k9r0nrctrkgjzpme0h8xaic2x2ud2\",\"randomString2\":\"ao9xcu131u2z58tjq8xu9iofszc6zdu691r6hwx4vl8wf7fkts\"}}";

            var envelope = target.Deserialize(json);

            Assert.IsTrue(envelope is Command);

            var command = (Command)envelope;

            
        }
    }
}
