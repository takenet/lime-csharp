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
using Shouldly;

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
        public void Serialize_AbsoluteUriRequestCommand_ReturnsValidJsonString()
        {
            var target = GetTarget();

            var command = DataUtil.CreateCommand();
            command.Pp = DataUtil.CreateNode();
            command.Method = CommandMethod.Get;
            command.Uri = DataUtil.CreateAbsoluteLimeUri();


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
            Assert.IsTrue(resultString.ContainsJsonProperty(Command.URI_KEY, command.Uri));


            Assert.IsTrue(resultString.ContainsJsonProperty(Command.METHOD_KEY, command.Method));
            Assert.IsTrue(resultString.ContainsJsonProperty(metadataKey1, metadataValue1));
            Assert.IsTrue(resultString.ContainsJsonProperty(metadataKey2, metadataValue2));

            Assert.IsFalse(resultString.ContainsJsonKey(Command.STATUS_KEY));
            Assert.IsFalse(resultString.ContainsJsonKey(Command.REASON_KEY));
            Assert.IsFalse(resultString.ContainsJsonKey(Command.TYPE_KEY));
            Assert.IsFalse(resultString.ContainsJsonKey(Command.RESOURCE_KEY));
        }

        [TestMethod]
        [TestCategory("Serialize")]
        public void Serialize_RelativeUriRequestCommand_ReturnsValidJsonString()
        {
            var target = GetTarget();

            var resource = DataUtil.CreateJsonDocument();

            var command = DataUtil.CreateCommand(resource);
            command.Pp = DataUtil.CreateNode();
            command.Method = CommandMethod.Set;
            command.Uri = DataUtil.CreateRelativeLimeUri();

            var resultString = target.Serialize(command);

            Assert.IsTrue(resultString.HasValidJsonStackedBrackets());
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.ID_KEY, command.Id));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.FROM_KEY, command.From));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.PP_KEY, command.Pp));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.TO_KEY, command.To));
            Assert.IsTrue(resultString.ContainsJsonProperty(Command.METHOD_KEY, command.Method));
            Assert.IsTrue(resultString.ContainsJsonProperty(Command.URI_KEY, command.Uri));
            Assert.IsTrue(resultString.ContainsJsonProperty(Command.METHOD_KEY, command.Method));
            Assert.IsTrue(resultString.ContainsJsonProperty(Command.TYPE_KEY, resource.GetMediaType()));
            Assert.IsTrue(resultString.ContainsJsonKey(Command.RESOURCE_KEY));

            Assert.IsFalse(resultString.ContainsJsonKey(Command.METADATA_KEY));
            Assert.IsFalse(resultString.ContainsJsonKey(Command.STATUS_KEY));
            Assert.IsFalse(resultString.ContainsJsonKey(Command.REASON_KEY));
        }


        [TestMethod]
        [TestCategory("Serialize")]
        public void Serialize_CapabilityRequestCommand_ReturnsValidJsonString()
        {
            var target = GetTarget();

            var resource = DataUtil.CreateCapability();
            var command = DataUtil.CreateCommand(resource);
            command.Pp = DataUtil.CreateNode();
            command.Method = CommandMethod.Get;

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
            Assert.IsTrue(resultString.ContainsJsonProperty(Message.CONTENT_KEY, content.Text));
            Assert.IsTrue(resultString.ContainsJsonProperty(metadataKey1, metadataValue1));
            Assert.IsTrue(resultString.ContainsJsonProperty(metadataKey2, metadataValue2));
        }


        /// <summary>
        /// TODO: JsonContent support
        /// </summary>
        [TestMethod]
        [TestCategory("Serialize")]
        [ExpectedException(typeof(NotSupportedException))]
        public void Serialize_UnknownJsonContentMessage_ReturnsValidJsonString()
        {
            var target = GetTarget();

            var content = DataUtil.CreateJsonDocument();
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

            foreach (var keyValuePair in content)
            {
                Assert.IsTrue(resultString.ContainsJsonProperty(keyValuePair.Key, keyValuePair.Value));
            }

            Assert.IsTrue(resultString.ContainsJsonProperty(metadataKey1, metadataValue1));
            Assert.IsTrue(resultString.ContainsJsonProperty(metadataKey2, metadataValue2));
        }

        [TestMethod]
        [TestCategory("Serialize")]
        public void Serialize_UnknownPlainContentMessage_ReturnsValidJsonString()
        {
            var target = GetTarget();

            var content = DataUtil.CreatePlainDocument();
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
            Assert.IsTrue(resultString.ContainsJsonProperty(Message.CONTENT_KEY, content.Value));
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
            Assert.IsTrue(resultString.ContainsJsonProperty(Message.CONTENT_KEY, content.Text));

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

            var contentType1 = DataUtil.CreateJsonMediaType();
            var contentType2 = DataUtil.CreateJsonMediaType();
            var contentType3 = DataUtil.CreateJsonMediaType();

            var resourceType1 = DataUtil.CreateJsonMediaType();
            var resourceType2 = DataUtil.CreateJsonMediaType();
            var resourceType3 = DataUtil.CreateJsonMediaType();

            var method = CommandMethod.Get;

            var id = Guid.NewGuid();

            var from = DataUtil.CreateNode();
            var pp = DataUtil.CreateNode();
            var to = DataUtil.CreateNode();

            string randomKey1 = "randomString1";
            string randomKey2 = "randomString2";
            string randomString1 = DataUtil.CreateRandomString(50);
            string randomString2 = DataUtil.CreateRandomString(50);

            var resourceUri = new LimeUri("/capability");

            string json = string.Format(
                "{{\"uri\":\"{0}\",\"type\":\"application/vnd.lime.capability+json\",\"resource\":{{\"contentTypes\":[\"{1}\",\"{2}\",\"{3}\"],\"resourceTypes\":[\"{4}\",\"{5}\",\"{6}\"]}},\"method\":\"{7}\",\"id\":\"{8}\",\"from\":\"{9}\",\"pp\":\"{10}\",\"to\":\"{11}\",\"metadata\":{{\"{12}\":\"{13}\",\"{14}\":\"{15}\"}}}}",
                resourceUri,
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

            Assert.IsNotNull(command.Uri);
            Assert.AreEqual(command.Uri, resourceUri);
        }

        [TestMethod]
        [TestCategory("Deserialize")]
        public void Deserialize_AbsoluteUriRequestCommand_ReturnsValidInstance()
        {
            var target = GetTarget();

            var method = CommandMethod.Get;

            var id = Guid.NewGuid();

            var from = DataUtil.CreateNode();
            var pp = DataUtil.CreateNode();
            var to = DataUtil.CreateNode();

            string randomKey1 = "randomString1";
            string randomKey2 = "randomString2";
            string randomString1 = DataUtil.CreateRandomString(50);
            string randomString2 = DataUtil.CreateRandomString(50);

            var resourceUri = DataUtil.CreateAbsoluteLimeUri();

            string json = string.Format(
                "{{\"uri\":\"{0}\",\"method\":\"get\",\"id\":\"{1}\",\"from\":\"{2}\",\"pp\":\"{3}\",\"to\":\"{4}\",\"metadata\":{{\"{5}\":\"{6}\",\"{7}\":\"{8}\"}}}}",
                resourceUri,
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

            Assert.IsNotNull(command.Uri);
            Assert.AreEqual(command.Uri, resourceUri);

            Assert.IsNull(command.Type);
            Assert.IsNull(command.Resource);
        }

        [TestMethod]
        [TestCategory("Deserialize")]
        public void Deserialize_ReceiptRequestCommand_ReturnsValidInstance()
        {
            var target = GetTarget();

            var method = CommandMethod.Set;
            var id = Guid.NewGuid();

            string json = string.Format(
                "{{\"type\":\"application/vnd.lime.receipt+json\",\"resource\":{{\"events\":[\"dispatched\",\"received\"]}},\"method\":\"{0}\",\"id\":\"{1}\"}}",
                method.ToString().ToCamelCase(),
                id);

            var envelope = target.Deserialize(json);

            Assert.IsTrue(envelope is Command);
            var command = (Command)envelope;
            Assert.AreEqual(id, command.Id);
            Assert.IsNull(command.From);
            Assert.IsNull(command.Pp);
            Assert.IsNull(command.To);

            Assert.AreEqual(method, command.Method);
            Assert.IsNull(command.Metadata);
            Assert.AreEqual(command.Type.ToString(), Receipt.MIME_TYPE);
            Assert.IsNotNull(command.Resource);
            Assert.IsTrue(command.Resource is Receipt);
        }

        [TestMethod]
        [TestCategory("Deserialize")]
        public void Deserialize_ContactCollectionResponseCommand_ReturnsValidInstance()
        {
            var target = GetTarget();

            var identity1 = DataUtil.CreateIdentity();
            var name1 = DataUtil.CreateRandomString(50);
            var identity2 = DataUtil.CreateIdentity();
            var name2 = DataUtil.CreateRandomString(50);
            var identity3 = DataUtil.CreateIdentity();
            var name3 = DataUtil.CreateRandomString(50);

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
                "{{\"type\":\"application/vnd.lime.collection+json\",\"resource\":{{\"itemType\":\"application/vnd.lime.contact+json\",\"total\":3,\"items\":[{{\"identity\":\"{0}\",\"name\":\"{1}\",\"isPending\":true,\"shareAccountInfo\":false}},{{\"identity\":\"{2}\",\"name\":\"{3}\",\"sharePresence\":false}},{{\"identity\":\"{4}\",\"name\":\"{5}\",\"isPending\":true,\"sharePresence\":false}}]}},\"method\":\"get\",\"status\":\"success\",\"id\":\"{6}\",\"from\":\"{7}\",\"pp\":\"{8}\",\"to\":\"{9}\",\"metadata\":{{\"{10}\":\"{11}\",\"{12}\":\"{13}\"}}}}",
                identity1,
                name1,
                identity2,
                name2,
                identity3,
                name3,
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

            Assert.IsTrue(command.Resource is DocumentCollection);

            var documents = (DocumentCollection)command.Resource;

            Assert.IsNotNull(documents.Items, "Items is null");
            Assert.AreEqual(documents.Items.Length, 3);

            var contacts = documents.Cast<Contact>().ToArray();

            Assert.IsTrue(contacts[0].Identity.Equals(identity1));
            Assert.IsTrue(contacts[0].Name.Equals(name1));
            Assert.IsTrue(contacts[0].IsPending);
            Assert.IsFalse(contacts[0].ShareAccountInfo);
            //Assert.IsTrue(contacts[0].SharePresence);

            Assert.IsTrue(contacts[1].Identity.Equals(identity2));
            Assert.IsTrue(contacts[1].Name.Equals(name2));
            Assert.IsFalse(contacts[1].IsPending);
            //Assert.IsTrue(contacts[1].ShareAccountInfo);
            Assert.IsFalse(contacts[1].SharePresence);

            Assert.IsTrue(contacts[2].Identity.Equals(identity3));
            Assert.IsTrue(contacts[2].Name.Equals(name3));
            Assert.IsTrue(contacts[2].IsPending);
            //Assert.IsTrue(contacts[2].ShareAccountInfo);
            Assert.IsFalse(contacts[2].SharePresence);


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
                "{{\"type\":\"text/plain\",\"content\":\"{0}\",\"id\":\"{1}\",\"from\":\"{2}\",\"pp\":\"{3}\",\"to\":\"{4}\",\"metadata\":{{\"{5}\":\"{6}\",\"{7}\":\"{8}\"}}}}",
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

            Assert.IsTrue(message.Content is PlainText);

            var textContent = (PlainText)message.Content;
            Assert.AreEqual(text, textContent.Text);
        }


        [TestMethod]
        [TestCategory("Deserialize")]
        public void Deserialize_ChatStateMessage_ReturnsValidInstance()
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

            var state = ChatStateEvent.Deleting;

            string json = string.Format(
                "{{\"type\":\"application/vnd.lime.chatstate+json\",\"content\":{{\"state\":\"{0}\"}},\"id\":\"{1}\",\"from\":\"{2}\",\"pp\":\"{3}\",\"to\":\"{4}\",\"metadata\":{{\"{5}\":\"{6}\",\"{7}\":\"{8}\"}}}}",
                state.ToString().ToLowerInvariant(),
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

            Assert.IsTrue(message.Content is ChatState);

            var textContent = (ChatState)message.Content;
            Assert.AreEqual(state, textContent.State);
        }



        [TestMethod]
        [TestCategory("Deserialize")]
        public void Deserialize_UnknownContentMessage_ReturnsValidInstance()
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


            var type = DataUtil.CreateJsonMediaType();

            var propertyName1 = DataUtil.CreateRandomString(10);
            var propertyName2 = DataUtil.CreateRandomString(10);
            var propertyValue1 = DataUtil.CreateRandomString(10);
            var propertyValue2 = DataUtil.CreateRandomInt(1000);


            string json = string.Format(
                "{{\"type\":\"{0}\",\"content\":{{\"{1}\":\"{2}\",\"{3}\":{4}}},\"id\":\"{5}\",\"from\":\"{6}\",\"pp\":\"{7}\",\"to\":\"{8}\",\"metadata\":{{\"{9}\":\"{10}\",\"{11}\":\"{12}\"}}}}",
                type,
                propertyName1,
                propertyValue1,
                propertyName2,
                propertyValue2,
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

            Assert.IsNotNull(message.Type);
            Assert.AreEqual(message.Type, type);

            Assert.IsTrue(message.Content is JsonDocument);

            var content = (JsonDocument)message.Content;

            Assert.IsTrue(content.ContainsKey(propertyName1));
            Assert.AreEqual(propertyValue1, content[propertyName1].ToString());
            Assert.IsTrue(content.ContainsKey(propertyName2));
            Assert.AreEqual(propertyValue2.ToString(), content[propertyName2].ToString());

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
                "{{\"type\":\"text/plain\",\"content\":\"{0}\",\"from\":\"{1}\",\"to\":\"{2}\"}}",
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

            Assert.IsTrue(message.Content is PlainText);
            var textContent = (PlainText)message.Content;
            Assert.AreEqual(text, textContent.Text);
        }


        [TestMethod]
        [TestCategory("Deserialize")]
        public void Deserialize_FireAndForgetChatStateMessage_ReturnsValidInstance()
        {
            var target = GetTarget();

            var from = DataUtil.CreateNode();
            var to = DataUtil.CreateNode();

            var state = ChatStateEvent.Composing;

            string json = string.Format(
                "{{\"type\":\"application/vnd.lime.chatstate+json\",\"content\":{{\"state\":\"{0}\"}},\"from\":\"{1}\",\"to\":\"{2}\"}}",
                state.ToString().ToCamelCase(),
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

            Assert.IsTrue(message.Content is ChatState);
            var textContent = (ChatState)message.Content;
            Assert.AreEqual(state, textContent.State);
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
        public void Deserialize_FailedSessionNullProperties_ReturnsValidInstance()
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
                "{{\"state\":\"{0}\",\"id\":\"{1}\",\"from\":\"{2}\",\"to\":\"{3}\",\"reason\":{{\"code\":{4},\"description\":\"{5}\"}},\"encryptionOptions\":null,\"compressionOptions\":null,\"compression\":null,\"encryption\":null}}",
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

        [TestMethod]
        [TestCategory("Deserialize")]
        public void Deserialize_SessionAuthenticatingWithPlainAuthentication_ReturnsValidInstance()
        {
            // Arrange
            var serializer = GetTarget();
            var json = "{\"state\":\"authenticating\",\"scheme\":\"plain\",\"authentication\":{\"password\":\"Zg==\"},\"id\":\"ec9c196c-da09-43b0-923b-8ec162705c32\",\"from\":\"andre@takenet.com.br/MINELLI-NOTE\"}";

            // Act
            var envelope = serializer.Deserialize(json);

            // Assert
            var session = envelope.ShouldBeOfType<Session>();
            session.Scheme.ShouldBe(AuthenticationScheme.Plain);
            var plainAuthentication = session.Authentication.ShouldBeOfType<PlainAuthentication>();
            plainAuthentication.Password.ShouldNotBeEmpty();
        }

        [TestMethod]
        [TestCategory("Deserialize")]
        public void Deserialize_SessionAuthenticatingWithGuestAuthentication_ReturnsValidInstance()
        {
            // Arrange
            var serializer = GetTarget();
            var json = "{\"state\":\"authenticating\",\"scheme\":\"guest\",\"id\":\"feeb88e2-c209-40cd-b8ab-e14aeebe57ab\",\"from\":\"ca6829ff-1ac8-4dad-ad78-c25a3e4f8f7b@takenet.com.br/MINELLI-NOTE\"}";

            // Act
            var envelope = serializer.Deserialize(json);

            // Assert
            var session = envelope.ShouldBeOfType<Session>();
            session.Scheme.ShouldBe(AuthenticationScheme.Guest);
            session.Authentication.ShouldBeOfType<GuestAuthentication>();
        }
        #endregion
    }
}