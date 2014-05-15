using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Lime.Protocol.Serialization;
using Lime.Protocol.UnitTests.Serialization;
using Lime.Protocol.Contents;
using System.Collections.Generic;
using Lime.Protocol.Security;
using Lime.Protocol.Resources;
using System.Text;
using Lime.Protocol.UnitTests;

namespace Lime.Protocol.Serialization.Newtonsoft.UnitTests
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
        public void Serialize_RosterResponseCommand_ReturnsValidJsonString()
        {
            var target = GetTarget();

            var resource = DataUtil.CreateRoster();
            var command = DataUtil.CreateCommand(resource);
            command.Pp = DataUtil.CreateNode();
            command.Method = CommandMethod.Get;
            command.Status = CommandStatus.Success;

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

            Assert.IsTrue(resultString.ContainsJsonKey(Roster.CONTACTS_KEY));
            Assert.IsTrue(resultString.ContainsJsonProperty(Contact.IDENTITY_KEY, resource.Contacts[0].Identity));
            Assert.IsTrue(resultString.ContainsJsonProperty(Contact.NAME_KEY, resource.Contacts[0].Name));
            Assert.IsTrue(resultString.ContainsJsonProperty(Contact.IS_PENDING_KEY, resource.Contacts[0].IsPending));
            Assert.IsTrue(resultString.ContainsJsonProperty(Contact.SHARE_ACCOUNT_INFO_KEY, resource.Contacts[0].ShareAccountInfo));
            Assert.IsTrue(resultString.ContainsJsonProperty(Contact.IDENTITY_KEY, resource.Contacts[1].Identity));
            Assert.IsTrue(resultString.ContainsJsonProperty(Contact.NAME_KEY, resource.Contacts[1].Name));
            Assert.IsTrue(resultString.ContainsJsonProperty(Contact.SHARE_PRESENCE_KEY, resource.Contacts[1].SharePresence));
            Assert.IsTrue(resultString.ContainsJsonProperty(Contact.IDENTITY_KEY, resource.Contacts[2].Identity));
            Assert.IsTrue(resultString.ContainsJsonProperty(Contact.NAME_KEY, resource.Contacts[2].Name));

            Assert.IsTrue(resultString.ContainsJsonKey(Command.STATUS_KEY));
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
            message.Id = Guid.Empty;

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

        #region Deserialize

        [TestMethod]
        [TestCategory("Deserialize")]
        public void Deserialize_CapabilityRequestCommand_ReturnsValidInstance()
        {
            var target = GetTarget();

            var contentType1 = DataUtil.CreateMediaType();
            var contentType2 = DataUtil.CreateMediaType();
            var contentType3 = DataUtil.CreateMediaType();

            var resourceType1 = DataUtil.CreateMediaType();
            var resourceType2 = DataUtil.CreateMediaType();
            var resourceType3 = DataUtil.CreateMediaType();

            var method = CommandMethod.Get;

            var id = Guid.NewGuid();

            var from = DataUtil.CreateNode();
            var pp = DataUtil.CreateNode();
            var to = DataUtil.CreateNode();

            string randomKey1 = "randomString1";
            string randomKey2 = "randomString2";
            string randomString1 = DataUtil.CreateRandomString(50);
            string randomString2 = DataUtil.CreateRandomString(50);

            string json = string.Format(
                "{{\"type\":\"application/vnd.lime.capability+json\",\"resource\":{{\"contentTypes\":[\"{0}\",\"{1}\",\"{2}\"],\"resourceTypes\":[\"{3}\",\"{4}\",\"{5}\"]}},\"method\":\"{6}\",\"id\":\"{7}\",\"from\":\"{8}\",\"pp\":\"{9}\",\"to\":\"{10}\",\"metadata\":{{\"{11}\":\"{12}\",\"{13}\":\"{14}\"}}}}",
                contentType1,
                contentType2,
                contentType3,
                resourceType1,
                resourceType2,
                resourceType3,
                method.ToString().ToCamelCase(),
                id,
                from,
                pp,
                to,
                randomKey1,
                randomString1,
                randomKey2,
                randomString2);

            var envelope = target.Deserialize(json);

            Assert.IsTrue(envelope is Command);
            var command = (Command)envelope;
            Assert.AreEqual(id, command.Id);
            Assert.AreEqual(from, command.From);
            Assert.AreEqual(pp, command.Pp);
            Assert.AreEqual(to, command.To);

            Assert.AreEqual(method, command.Method);
            Assert.IsNotNull(command.Metadata);
            Assert.IsTrue(command.Metadata.ContainsKey(randomKey1));
            Assert.AreEqual(command.Metadata[randomKey1], randomString1);
            Assert.IsTrue(command.Metadata.ContainsKey(randomKey2));
            Assert.AreEqual(command.Metadata[randomKey2], randomString2);

            Assert.IsTrue(command.Resource is Capability);

            var capability = (Capability)command.Resource;

            Assert.IsTrue(capability.ContentTypes.Any(c => c.Equals(contentType1)));
            Assert.IsTrue(capability.ContentTypes.Any(c => c.Equals(contentType2)));
            Assert.IsTrue(capability.ContentTypes.Any(c => c.Equals(contentType3)));

            Assert.IsTrue(capability.ResourceTypes.Any(c => c.Equals(resourceType1)));
            Assert.IsTrue(capability.ResourceTypes.Any(c => c.Equals(resourceType2)));
            Assert.IsTrue(capability.ResourceTypes.Any(c => c.Equals(resourceType3)));
        }


        [TestMethod]
        [TestCategory("Deserialize")]
        public void Deserialize_FailureCapabilityResponseCommand_ReturnsValidInstance()
        {
            var target = GetTarget();

            var method = CommandMethod.Get;
            var status = CommandStatus.Failure;
            var reason = DataUtil.CreateReason();
            var id = Guid.NewGuid();
            var from = DataUtil.CreateNode();
            var to = DataUtil.CreateNode();

            string json = string.Format(
                "{{\"method\":\"{0}\",\"id\":\"{1}\",\"from\":\"{2}\",\"to\":\"{3}\",\"status\":\"{4}\",\"reason\":{{\"code\":{5},\"description\":\"{6}\"}}}}",
                method.ToString().ToCamelCase(),
                id,
                from,
                to,
                status,
                reason.Code,
                reason.Description);

            var envelope = target.Deserialize(json);

            Assert.IsTrue(envelope is Command);
            var command = (Command)envelope;
            Assert.AreEqual(id, command.Id);
            Assert.AreEqual(from, command.From);
            Assert.AreEqual(to, command.To);
            Assert.AreEqual(method, command.Method);

            Assert.IsNull(command.Pp);
            Assert.IsNull(command.Metadata);
            Assert.IsNull(command.Type);
            Assert.IsNull(command.Resource);

            Assert.IsNotNull(command.Reason);

            Assert.AreEqual(reason.Code, command.Reason.Code);
            Assert.AreEqual(reason.Description, command.Reason.Description);
        }

        [TestMethod]
        [TestCategory("Deserialize")]
        public void Deserialize_TextMessage_ReturnsValidInstance()
        {
            var target = GetTarget();

            var id = Guid.NewGuid();
            var from = DataUtil.CreateNode();
            var pp = DataUtil.CreateNode();
            var to = DataUtil.CreateNode();
            
            string randomKey1 = "randomString1";
            string randomKey2 = "randomString2";
            string randomString1 = DataUtil.CreateRandomString(50);
            string randomString2 = DataUtil.CreateRandomString(50);

            var text = DataUtil.CreateRandomString(50);

            string json = string.Format(
                "{{\"type\":\"application/vnd.lime.text+json\",\"content\":{{\"text\":\"{0}\"}},\"id\":\"{1}\",\"from\":\"{2}\",\"pp\":\"{3}\",\"to\":\"{4}\",\"metadata\":{{\"{5}\":\"{6}\",\"{7}\":\"{8}\"}}}}",
                text,
                id,
                from,
                pp,
                to,
                randomKey1,
                randomString1,
                randomKey2,
                randomString2
                );

            var envelope = target.Deserialize(json);

            Assert.IsTrue(envelope is Message);

            var message = (Message)envelope;
            Assert.AreEqual(id, message.Id);
            Assert.AreEqual(from, message.From);
            Assert.AreEqual(pp, message.Pp);
            Assert.AreEqual(to, message.To);
            Assert.IsNotNull(message.Metadata);
            Assert.IsTrue(message.Metadata.ContainsKey(randomKey1));
            Assert.AreEqual(message.Metadata[randomKey1], randomString1);
            Assert.IsTrue(message.Metadata.ContainsKey(randomKey2));
            Assert.AreEqual(message.Metadata[randomKey2], randomString2);

            Assert.IsTrue(message.Content is TextContent);

            var textContent = (TextContent)message.Content;
            Assert.AreEqual(text, textContent.Text);
        }

        [TestMethod]
        [TestCategory("Deserialize")]
        public void Deserialize_FireAndForgetTextMessage_ReturnsValidInstance()
        {
            var target = GetTarget();

            var from = DataUtil.CreateNode();
            var to = DataUtil.CreateNode();

            var text = DataUtil.CreateRandomString(50);

            string json = string.Format(
                "{{\"type\":\"application/vnd.lime.text+json\",\"content\":{{\"text\":\"{0}\"}},\"from\":\"{1}\",\"to\":\"{2}\"}}",
                text,
                from,
                to
                );

            var envelope = target.Deserialize(json);

            Assert.IsTrue(envelope is Message);

            var message = (Message)envelope;
            Assert.AreEqual(from, message.From);
            Assert.AreEqual(to, message.To);

            Assert.AreEqual(message.Id, Guid.Empty);
            Assert.IsNull(message.Pp);
            Assert.IsNull(message.Metadata);

            Assert.IsTrue(message.Content is TextContent);
            var textContent = (TextContent)message.Content;
            Assert.AreEqual(text, textContent.Text);            
        }

        [TestMethod]
        [TestCategory("Deserialize")]
        public void Deserialize_ReceivedNotification_ReturnsValidInstance()
        {
            var target = GetTarget();

            var id = Guid.NewGuid();
            var from = DataUtil.CreateNode();
            var pp = DataUtil.CreateNode();
            var to = DataUtil.CreateNode();

            string randomKey1 = "randomString1";
            string randomKey2 = "randomString2";
            string randomString1 = DataUtil.CreateRandomString(50);
            string randomString2 = DataUtil.CreateRandomString(50);

            var @event = Event.Received;

            string json = string.Format(
                "{{\"type\":\"application/vnd.lime.text+json\",\"event\":\"{0}\",\"id\":\"{1}\",\"from\":\"{2}\",\"pp\":\"{3}\",\"to\":\"{4}\",\"metadata\":{{\"{5}\":\"{6}\",\"{7}\":\"{8}\"}}}}",
                @event.ToString().ToCamelCase(),
                id,
                from,
                pp,
                to,
                randomKey1,
                randomString1,
                randomKey2,
                randomString2
                );

            var envelope = target.Deserialize(json);

            Assert.IsTrue(envelope is Notification);

            var notification = (Notification)envelope;
            Assert.AreEqual(id, notification.Id);
            Assert.AreEqual(from, notification.From);
            Assert.AreEqual(pp, notification.Pp);
            Assert.AreEqual(to, notification.To);
            Assert.IsNotNull(notification.Metadata);
            Assert.IsTrue(notification.Metadata.ContainsKey(randomKey1));
            Assert.AreEqual(notification.Metadata[randomKey1], randomString1);
            Assert.IsTrue(notification.Metadata.ContainsKey(randomKey2));
            Assert.AreEqual(notification.Metadata[randomKey2], randomString2);

            Assert.AreEqual(@event, notification.Event);

            Assert.IsNull(notification.Reason);
        }

        [TestMethod]
        [TestCategory("Deserialize")]
        public void Deserialize_FailedNotification_ReturnsValidInstance()
        {
            var target = GetTarget();

            var @event = Event.Received;

            var reasonCode = DataUtil.CreateRandomInt(100);
            var reasonDescription = DataUtil.CreateRandomString(100);

            var id = Guid.NewGuid();
            var from = DataUtil.CreateNode();
            var to = DataUtil.CreateNode();

            string json = string.Format(
                "{{\"event\":\"{0}\",\"id\":\"{1}\",\"from\":\"{2}\",\"to\":\"{3}\",\"reason\":{{\"code\":{4},\"description\":\"{5}\"}}}}",
                @event.ToString().ToCamelCase(),
                id,
                from,
                to,
                reasonCode,
                reasonDescription);

            var envelope = target.Deserialize(json);

            Assert.IsTrue(envelope is Notification);
            var notification = (Notification)envelope;
            Assert.AreEqual(id, notification.Id);
            Assert.AreEqual(from, notification.From);
            Assert.AreEqual(to, notification.To);
            Assert.AreEqual(@event, notification.Event);

            Assert.IsNull(notification.Pp);
            Assert.IsNull(notification.Metadata);

            Assert.IsNotNull(notification.Reason);

            Assert.AreEqual(reasonCode, notification.Reason.Code);
            Assert.AreEqual(reasonDescription, notification.Reason.Description);
        }

        [TestMethod]
        [TestCategory("Deserialize")]
        public void Deserialize_AuthenticatingSession_ReturnsValidInstance()
        {
            var target = GetTarget();

            var id = Guid.NewGuid();
            var from = DataUtil.CreateNode();
            var pp = DataUtil.CreateNode();
            var to = DataUtil.CreateNode();

            var password = DataUtil.CreateRandomString(10).ToBase64();

            string randomKey1 = "randomString1";
            string randomKey2 = "randomString2";
            string randomString1 = DataUtil.CreateRandomString(50);
            string randomString2 = DataUtil.CreateRandomString(50);

            var state = SessionState.Authenticating;

            string json = string.Format(
                "{{\"state\":\"{0}\",\"scheme\":\"plain\",\"authentication\":{{\"password\":\"{1}\"}},\"id\":\"{2}\",\"from\":\"{3}\",\"to\":\"{4}\",\"metadata\":{{\"{5}\":\"{6}\",\"{7}\":\"{8}\"}}}}",
                state.ToString().ToCamelCase(),
                password,
                id,
                from,
                to,
                randomKey1,
                randomString1,
                randomKey2,
                randomString2
                );

            var envelope = target.Deserialize(json);

            Assert.IsTrue(envelope is Session);

            var session = (Session)envelope;
            Assert.AreEqual(id, session.Id);
            Assert.AreEqual(from, session.From);
            Assert.AreEqual(to, session.To);
            Assert.IsNotNull(session.Metadata);
            Assert.IsTrue(session.Metadata.ContainsKey(randomKey1));
            Assert.AreEqual(session.Metadata[randomKey1], randomString1);
            Assert.IsTrue(session.Metadata.ContainsKey(randomKey2));
            Assert.AreEqual(session.Metadata[randomKey2], randomString2);

            Assert.AreEqual(state, session.State);

            Assert.IsNull(session.Pp);
            Assert.IsNull(session.Reason);
        }

        [TestMethod]
        [TestCategory("Deserialize")]
        public void Deserialize_FailedSession_ReturnsValidInstance()
        {
            var target = GetTarget();

            var id = Guid.NewGuid();
            var from = DataUtil.CreateNode();
            var pp = DataUtil.CreateNode();
            var to = DataUtil.CreateNode();

            var password = DataUtil.CreateRandomString(10).ToBase64();

            var state = SessionState.Authenticating;

            var reasonCode = DataUtil.CreateRandomInt(100);
            var reasonDescription = DataUtil.CreateRandomString(100);

            string json = string.Format(
                "{{\"state\":\"{0}\",\"id\":\"{1}\",\"from\":\"{2}\",\"to\":\"{3}\",\"reason\":{{\"code\":{4},\"description\":\"{5}\"}}}}",
                state.ToString().ToCamelCase(),
                id,
                from,
                to,
                reasonCode,
                reasonDescription
                );

            var envelope = target.Deserialize(json);

            Assert.IsTrue(envelope is Session);

            var session = (Session)envelope;
            Assert.AreEqual(id, session.Id);
            Assert.AreEqual(from, session.From);
            Assert.AreEqual(to, session.To);

            Assert.AreEqual(state, session.State);

            Assert.IsNotNull(session.Reason);
            Assert.AreEqual(reasonCode, session.Reason.Code);
            Assert.AreEqual(reasonDescription, session.Reason.Description);

            Assert.IsNull(session.Pp);
            Assert.IsNull(session.Metadata);
        }

        #endregion


    }
}