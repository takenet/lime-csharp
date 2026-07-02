using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lime.Messaging;
using Lime.Messaging.Contents;
using Lime.Messaging.Resources;
using Lime.Protocol.Security;
using Lime.Protocol.Serialization;
using Lime.Protocol.Serialization.SystemTextJson;
using Lime.Protocol.UnitTests.Serialization.Models;
using NUnit.Framework;
using Shouldly;

namespace Lime.Protocol.UnitTests.Serialization.SystemTextJson
{
    [TestFixture]
    public class EnvelopeSerializerTests
    {
        public IDocumentTypeResolver DocumentTypeResolver;

        private EnvelopeSerializer GetTarget()
        {
            DocumentTypeResolver = new DocumentTypeResolver().WithMessagingDocuments();
            DocumentTypeResolver.RegisterDocument(typeof(STJDummyObject1));
            DocumentTypeResolver.RegisterDocument(typeof(STJDummyObject2));
            return new EnvelopeSerializer(DocumentTypeResolver);
        }

        private static Envelope Deserialize<T>(EnvelopeSerializer serializer, string json, DeserializeMethod method) where T : Envelope
        {
            if (method == DeserializeMethod.String) return serializer.Deserialize(json) as T;
            if (method == DeserializeMethod.Stream)
            {
                using var memoryStream = new MemoryStream();
                using var writer = new StreamWriter(memoryStream);
                writer.Write(json);
                writer.Flush();
                memoryStream.Position = 0;
                using var reader = new StreamReader(memoryStream);
                return serializer.Deserialize<T>(reader);
            }
            throw new NotImplementedException();
        }

        public enum DeserializeMethod
        {
            String,
            Stream
        }

        #region Serialize

        [Test]
        [Category("Serialize")]
        public void Serialize_AbsoluteUriRequestCommand_ReturnsValidJsonString()
        {
            var target = GetTarget();

            var command = Dummy.CreateCommand();
            command.Pp = Dummy.CreateNode();
            command.Method = CommandMethod.Get;
            command.Uri = Dummy.CreateAbsoluteLimeUri();

            var metadataKey1 = "randomString1";
            var metadataValue1 = Dummy.CreateRandomString(50);
            var metadataKey2 = "randomString2";
            var metadataValue2 = Dummy.CreateRandomString(50);
            command.Metadata = new Dictionary<string, string>
            {
                [metadataKey1] = metadataValue1,
                [metadataKey2] = metadataValue2
            };

            var resultString = target.Serialize(command);

            Assert.IsTrue(resultString.HasValidJsonStackedBrackets());
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.ID_KEY, command.Id));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.FROM_KEY, command.From));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.PP_KEY, command.Pp));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.TO_KEY, command.To));
            Assert.IsTrue(resultString.ContainsJsonProperty(Command.METHOD_KEY, command.Method));
            Assert.IsTrue(resultString.ContainsJsonProperty(Command.URI_KEY, command.Uri));
            Assert.IsTrue(resultString.ContainsJsonProperty(metadataKey1, metadataValue1));
            Assert.IsTrue(resultString.ContainsJsonProperty(metadataKey2, metadataValue2));

            Assert.IsFalse(resultString.ContainsJsonKey(Command.STATUS_KEY));
            Assert.IsFalse(resultString.ContainsJsonKey(Command.REASON_KEY));
            Assert.IsFalse(resultString.ContainsJsonKey(Command.TYPE_KEY));
            Assert.IsFalse(resultString.ContainsJsonKey(Command.RESOURCE_KEY));
        }

        [Test]
        [Category("Serialize")]
        public void Serialize_TextMessage_ReturnsValidJsonString()
        {
            var target = GetTarget();

            var content = Dummy.CreateTextContent();
            var message = Dummy.CreateMessage(content);
            message.Pp = Dummy.CreateNode();

            var metadataKey1 = "randomString1";
            var metadataValue1 = Dummy.CreateRandomString(50);
            var metadataKey2 = "randomString2";
            var metadataValue2 = Dummy.CreateRandomString(50);
            message.Metadata = new Dictionary<string, string>
            {
                [metadataKey1] = metadataValue1,
                [metadataKey2] = metadataValue2
            };

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

        [Test]
        [Category("Serialize")]
        public void Serialize_UnknownPlainContentMessage_ReturnsValidJsonString()
        {
            var target = GetTarget();

            var content = Dummy.CreatePlainDocument();
            var message = Dummy.CreateMessage(content);
            message.Pp = Dummy.CreateNode();

            var resultString = target.Serialize(message);

            Assert.IsTrue(resultString.HasValidJsonStackedBrackets());
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.ID_KEY, message.Id));
            Assert.IsTrue(resultString.ContainsJsonProperty(Message.TYPE_KEY, message.Content.GetMediaType()));
            Assert.IsTrue(resultString.ContainsJsonKey(Message.CONTENT_KEY));
            Assert.IsTrue(resultString.ContainsJsonProperty(Message.CONTENT_KEY, content.Value));
        }

        [Test]
        [Category("Serialize")]
        public void Serialize_UnknownJsonContentMessage_ReturnsValidJsonString()
        {
            var target = GetTarget();

            var content = Dummy.CreateJsonDocument();
            var message = Dummy.CreateMessage(content);

            var resultString = target.Serialize(message);

            Assert.IsTrue(resultString.HasValidJsonStackedBrackets());
            Assert.IsTrue(resultString.ContainsJsonProperty(Message.TYPE_KEY, message.Content.GetMediaType()));
            Assert.IsTrue(resultString.ContainsJsonKey(Message.CONTENT_KEY));

            foreach (var kvp in content)
            {
                if (!kvp.Value.GetType().IsArray)
                {
                    Assert.IsTrue(resultString.ContainsJsonProperty(kvp.Key, kvp.Value));
                }
            }
        }

        [Test]
        [Category("Serialize")]
        public void Serialize_FireAndForgetTextMessage_ReturnsValidJsonString()
        {
            var target = GetTarget();

            var content = Dummy.CreateTextContent();
            var message = Dummy.CreateMessage(content);
            message.Id = null;

            var resultString = target.Serialize(message);

            Assert.IsTrue(resultString.HasValidJsonStackedBrackets());
            Assert.IsTrue(resultString.ContainsJsonProperty(Message.TYPE_KEY, message.Content.GetMediaType()));
            Assert.IsTrue(resultString.ContainsJsonProperty(Message.CONTENT_KEY, content.Text));

            Assert.IsFalse(resultString.ContainsJsonKey(Envelope.ID_KEY));
            Assert.IsFalse(resultString.ContainsJsonKey(Envelope.PP_KEY));
            Assert.IsFalse(resultString.ContainsJsonKey(Envelope.METADATA_KEY));
        }

        [Test]
        [Category("Serialize")]
        public void Serialize_ReceivedNotification_ReturnsValidJsonString()
        {
            var target = GetTarget();

            var notification = Dummy.CreateNotification(Event.Received);
            notification.Id = EnvelopeId.NewId();
            notification.Pp = Dummy.CreateNode();

            var metadataKey1 = "randomString1";
            var metadataValue1 = Dummy.CreateRandomString(50);
            notification.Metadata = new Dictionary<string, string> { [metadataKey1] = metadataValue1 };

            var resultString = target.Serialize(notification);

            Assert.IsTrue(resultString.HasValidJsonStackedBrackets());
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.ID_KEY, notification.Id));
            Assert.IsTrue(resultString.ContainsJsonProperty(Notification.EVENT_KEY, notification.Event));
            Assert.IsTrue(resultString.ContainsJsonProperty(metadataKey1, metadataValue1));
            Assert.IsFalse(resultString.ContainsJsonKey(Notification.REASON_KEY));
        }

        [Test]
        [Category("Serialize")]
        public void Serialize_FailedNotification_ReturnsValidJsonString()
        {
            var target = GetTarget();

            var notification = Dummy.CreateNotification(Event.Failed);
            notification.Id = EnvelopeId.NewId();
            notification.Reason = Dummy.CreateReason();

            var resultString = target.Serialize(notification);

            Assert.IsTrue(resultString.HasValidJsonStackedBrackets());
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.ID_KEY, notification.Id));
            Assert.IsTrue(resultString.ContainsJsonProperty(Notification.EVENT_KEY, notification.Event));
            Assert.IsTrue(resultString.ContainsJsonProperty(Reason.CODE_KEY, notification.Reason.Code));
            Assert.IsTrue(resultString.ContainsJsonProperty(Reason.DESCRIPTION_KEY, notification.Reason.Description));
        }

        [Test]
        [Category("Serialize")]
        public void Serialize_AuthenticatingSession_ReturnsValidJsonString()
        {
            var target = GetTarget();

            var session = Dummy.CreateSession();
            var plainAuthentication = Dummy.CreatePlainAuthentication();
            session.Authentication = plainAuthentication;
            session.State = SessionState.Authenticating;

            var metadataKey1 = "randomString1";
            var metadataValue1 = Dummy.CreateRandomString(50);
            session.Metadata = new Dictionary<string, string> { [metadataKey1] = metadataValue1 };

            var resultString = target.Serialize(session);

            Assert.IsTrue(resultString.HasValidJsonStackedBrackets());
            Assert.IsTrue(resultString.ContainsJsonProperty(Session.STATE_KEY, session.State));
            Assert.IsTrue(resultString.ContainsJsonKey(Session.AUTHENTICATION_KEY));
            Assert.IsTrue(resultString.ContainsJsonProperty(PlainAuthentication.PASSWORD_KEY, plainAuthentication.Password));
            Assert.IsFalse(resultString.ContainsJsonKey(Session.REASON_KEY));
        }

        [Test]
        [Category("Serialize")]
        public void Serialize_FailedSession_ReturnsValidJsonString()
        {
            var target = GetTarget();

            var session = Dummy.CreateSession();
            session.State = SessionState.Failed;
            session.Reason = Dummy.CreateReason();

            var resultString = target.Serialize(session);

            Assert.IsTrue(resultString.HasValidJsonStackedBrackets());
            Assert.IsTrue(resultString.ContainsJsonProperty(Session.STATE_KEY, session.State));
            Assert.IsTrue(resultString.ContainsJsonKey(Session.REASON_KEY));
            Assert.IsTrue(resultString.ContainsJsonProperty(Reason.CODE_KEY, session.Reason.Code));
            Assert.IsTrue(resultString.ContainsJsonProperty(Reason.DESCRIPTION_KEY, session.Reason.Description));
            Assert.IsFalse(resultString.ContainsJsonKey(Session.AUTHENTICATION_KEY));
        }

        [Test]
        [Category("Serialize")]
        public void Serialize_FailurePingResponseCommand_ReturnsValidJsonString()
        {
            var target = GetTarget();

            var command = Dummy.CreateCommand(status: CommandStatus.Failure);
            command.Reason = Dummy.CreateReason();

            var resultString = target.Serialize(command);

            Assert.IsTrue(resultString.HasValidJsonStackedBrackets());
            Assert.IsTrue(resultString.ContainsJsonProperty(Command.METHOD_KEY, command.Method));
            Assert.IsTrue(resultString.ContainsJsonProperty(Command.STATUS_KEY, command.Status));
            Assert.IsTrue(resultString.ContainsJsonKey(Command.REASON_KEY));
            Assert.IsFalse(resultString.ContainsJsonKey(Command.RESOURCE_KEY));
        }

        [Test]
        [Category("Serialize")]
        public void Serialize_ContactCollectionResponseCommand_ReturnsValidJsonString()
        {
            var target = GetTarget();

            var contact1 = Dummy.CreateContact();
            contact1.ShareAccountInfo = true;
            contact1.SharePresence = true;
            var contact2 = Dummy.CreateContact();
            contact2.SharePresence = true;
            var contact3 = Dummy.CreateContact();

            var resource = Dummy.CreateDocumentCollection<Contact>(contact1, contact2, contact3);

            var command = Dummy.CreateCommand(resource);
            command.Method = CommandMethod.Get;
            command.Status = CommandStatus.Success;

            var resultString = target.Serialize(command);

            Assert.IsTrue(resultString.HasValidJsonStackedBrackets());
            Assert.IsTrue(resultString.ContainsJsonKey(DocumentCollection.ITEMS_KEY));
            Assert.IsTrue(resultString.ContainsJsonProperty(DocumentCollection.ITEM_TYPE_KEY, contact1.GetMediaType()));
            Assert.IsTrue(resultString.ContainsJsonProperty(DocumentCollection.TOTAL_KEY, resource.Items.Length));
            Assert.IsTrue(resultString.ContainsJsonKey(Command.STATUS_KEY));
        }

        [Test]
        [Category("Serialize")]
        public void Serialize_PresenceRequestCommand_ReturnsValidJsonString()
        {
            var target = GetTarget();

            var resource = Dummy.CreatePresence();
            var command = Dummy.CreateCommand(resource);
            command.Method = CommandMethod.Get;

            var resultString = target.Serialize(command);

            Assert.IsTrue(resultString.HasValidJsonStackedBrackets());
            Assert.IsTrue(resultString.ContainsJsonProperty(Command.TYPE_KEY, command.Resource.GetMediaType()));
            Assert.IsTrue(resultString.ContainsJsonKey(Command.RESOURCE_KEY));
            Assert.IsTrue(resultString.ContainsJsonProperty(Presence.MESSAGE_KEY, resource.Message));
            Assert.IsTrue(resultString.ContainsJsonProperty(Presence.LAST_SEEN_KEY, resource.LastSeen));
            Assert.IsFalse(resultString.ContainsJsonProperty(Command.STATUS_KEY, "pending"));
        }

        [Test]
        [Category("Serialize")]
        public void Serialize_PresenceRequestWithOffsetDateCommand_ReturnsValidJsonString()
        {
            var target = GetTarget();

            var resource = Dummy.CreatePresence();
            resource.LastSeen = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(-5));
            var command = Dummy.CreateCommand(resource);
            command.Method = CommandMethod.Get;

            var resultString = target.Serialize(command);

            Assert.IsTrue(resultString.HasValidJsonStackedBrackets());
            Assert.IsTrue(resultString.ContainsJsonProperty(Presence.MESSAGE_KEY, resource.Message));
            Assert.IsTrue(resultString.ContainsJsonProperty(Presence.LAST_SEEN_KEY, resource.LastSeen));
        }

        [Test]
        [Category("Serialize")]
        public void Serialize_ChatStateMessage_ReturnsValidJsonString()
        {
            var chatState = Dummy.CreateChatState();
            var message = Dummy.CreateMessage(chatState);
            var target = GetTarget();

            var resultString = target.Serialize(message);

            Assert.IsTrue(resultString.HasValidJsonStackedBrackets());
            Assert.IsTrue(resultString.ContainsJsonProperty(ChatState.STATE_KEY, chatState.State));
        }

        [Test]
        [Category("Serialize")]
        public void Serialize_SelectMessage_ReturnsValidJsonString()
        {
            var select = Dummy.CreateSelect();
            var message = Dummy.CreateMessage(select);
            var target = GetTarget();

            var resultString = target.Serialize(message);

            Assert.IsTrue(resultString.HasValidJsonStackedBrackets());
            Assert.IsTrue(resultString.ContainsJsonProperty(Select.TEXT_KEY, select.Text));
            Assert.IsTrue(resultString.ContainsJsonKey(Select.OPTIONS_KEY));
        }

        [Test]
        [Category("Serialize")]
        public void Serialize_IdentityDocumentMessage_ReturnsValidJsonString()
        {
            var identityDocument = Dummy.CreateIdentityDocument();
            var message = Dummy.CreateMessage(identityDocument);
            var target = GetTarget();

            var resultString = target.Serialize(message);

            Assert.IsTrue(resultString.HasValidJsonStackedBrackets());
            Assert.IsTrue(resultString.ContainsJsonProperty(Message.TYPE_KEY, message.Content.GetMediaType()));
            Assert.IsTrue(resultString.ContainsJsonProperty(Message.CONTENT_KEY, identityDocument.Value));
        }

        [Test]
        [Category("Serialize")]
        public void Serialize_RandomDocument_ReturnsValidJsonString()
        {
            var resource = new TestDocument
            {
                Date = new DateTimeOffset(2022, 01, 28, 1, 2, 3, TimeSpan.Zero).UtcDateTime,
                Double = 44.9,
                Status = TestEnum.Success,
                Address = "limeTest@limeprotocol.org/instance"
            };

            var command = new Command()
            {
                Resource = resource,
                Id = EnvelopeId.NewId(),
                From = new Node("limeUser", "limeprotocol.org", null),
                To = new Node("limeUser", "limeprotocol.org", null),
                Method = CommandMethod.Set,
            };
            var target = GetTarget();

            var resultString = target.Serialize(command);

            Assert.IsTrue(resultString.HasValidJsonStackedBrackets());
            Assert.IsTrue(resultString.ContainsJsonProperty("date", "2022-01-28T01:02:03.000Z"));
            Assert.IsTrue(resultString.ContainsJsonProperty("double", 44.9));
        }

        [Test]
        [Category("Serialize")]
        public void Serialize_Deserialize_DocumentWithEnvelope()
        {
            var schedule = new Schedule();
            schedule.When = DateTimeOffset.Now.AddDays(1);
            schedule.Message = new Message()
            {
                Id = EnvelopeId.NewId(),
                Content = new PlainText() { Text = "Teste 5" },
                From = new Node("limeUser", "limeprotocol.org", null),
                To = new Node("limeUser", "limeprotocol.org", null)
            };

            var command = new Command()
            {
                Resource = schedule,
                Id = EnvelopeId.NewId(),
                From = new Node("limeUser", "limeprotocol.org", null),
                To = new Node("limeUser", "limeprotocol.org", null),
                Method = CommandMethod.Set,
                Uri = new LimeUri("/scheduler")
            };

            var serializer = new EnvelopeSerializer(new DocumentTypeResolver().WithMessagingDocuments());
            var json = serializer.Serialize(command);
            var deserializedCommand = (Command)serializer.Deserialize(json);
            deserializedCommand.ShouldNotBeNull();
        }

        [Test]
        [Category("Serialize")]
        public void Serialize_DocumentContainerDocumentCollectionMessage_ReturnsValidJsonString()
        {
            var document1 = Dummy.CreateTextContent();
            var document2 = Dummy.CreatePresence();
            var document3 = Dummy.CreateJsonDocument();
            var document4 = Dummy.CreatePlainDocument();
            var container1 = Dummy.CreateDocumentContainer(document1);
            var container2 = Dummy.CreateDocumentContainer(document2);
            var container3 = Dummy.CreateDocumentContainer(document3);
            var container4 = Dummy.CreateDocumentContainer(document4);
            var collection = Dummy.CreateDocumentCollection(container1, container2, container3, container4);
            var message = Dummy.CreateMessage(collection);
            var target = GetTarget();

            var resultString = target.Serialize(message);

            Assert.IsTrue(resultString.HasValidJsonStackedBrackets());
            Assert.IsTrue(resultString.ContainsJsonProperty(DocumentContainer.VALUE_KEY, document1.Text));
            Assert.IsTrue(resultString.ContainsJsonProperty(Presence.MESSAGE_KEY, document2.Message));
            Assert.IsTrue(resultString.ContainsJsonProperty(DocumentContainer.VALUE_KEY, container4.Value));
        }

        #endregion

        #region Deserialize

        [Test]
        [Category("Deserialize")]
        public void Deserialize_TextMessage_ReturnsValidInstance([Values(DeserializeMethod.String, DeserializeMethod.Stream)] DeserializeMethod deserializeMethod)
        {
            var target = GetTarget();

            var id = EnvelopeId.NewId();
            var from = Dummy.CreateNode();
            var pp = Dummy.CreateNode();
            var to = Dummy.CreateNode();

            var randomKey1 = "randomString1";
            var randomKey2 = "randomString2";
            var randomString1 = Dummy.CreateRandomStringExtended(50);
            var randomString2 = Dummy.CreateRandomStringExtended(50);

            var text = Dummy.CreateRandomStringExtended(50);

            string json =
                $"{{\"type\":\"text/plain\",\"content\":\"{text.Escape()}\",\"id\":\"{id}\",\"from\":\"{@from}\",\"pp\":\"{pp}\",\"to\":\"{to}\",\"metadata\":{{\"{randomKey1}\":\"{randomString1.Escape()}\",\"{randomKey2}\":\"{randomString2.Escape()}\"}}}}";

            var envelope = Deserialize<Message>(target, json, deserializeMethod);

            var message = envelope.ShouldBeOfType<Message>();
            Assert.AreEqual(id, message.Id);
            Assert.AreEqual(from, message.From);
            Assert.AreEqual(pp, message.Pp);
            Assert.AreEqual(to, message.To);
            Assert.IsNotNull(message.Metadata);
            Assert.AreEqual(message.Metadata[randomKey1], randomString1);
            Assert.AreEqual(message.Metadata[randomKey2], randomString2);

            var textContent = message.Content.ShouldBeOfType<PlainText>();
            Assert.AreEqual(text, textContent.Text);
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_FireAndForgetTextMessage_ReturnsValidInstance([Values(DeserializeMethod.String, DeserializeMethod.Stream)] DeserializeMethod deserializeMethod)
        {
            var target = GetTarget();
            var from = Dummy.CreateNode();
            var to = Dummy.CreateNode();
            var text = Dummy.CreateRandomStringExtended(50);
            string json =
                $"{{\"type\":\"text/plain\",\"content\":\"{text.Escape()}\",\"from\":\"{@from}\",\"to\":\"{to}\"}}";

            var envelope = Deserialize<Message>(target, json, deserializeMethod);

            var message = envelope.ShouldBeOfType<Message>();
            Assert.AreEqual(from, message.From);
            Assert.AreEqual(to, message.To);
            Assert.IsNull(message.Id);
            Assert.IsNull(message.Pp);
            Assert.IsNull(message.Metadata);
            var textContent = message.Content.ShouldBeOfType<PlainText>();
            Assert.AreEqual(text, textContent.Text);
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_UnknownPlainContentMessage_ReturnsValidInstance([Values(DeserializeMethod.String, DeserializeMethod.Stream)] DeserializeMethod deserializeMethod)
        {
            var target = GetTarget();

            var id = EnvelopeId.NewId();
            var from = Dummy.CreateNode();
            var pp = Dummy.CreateNode();
            var to = Dummy.CreateNode();
            var type = Dummy.CreatePlainMediaType();
            var text = Dummy.CreateRandomString(50);

            string json =
                $"{{\"type\":\"{type}\",\"content\":\"{text}\",\"id\":\"{id}\",\"from\":\"{@from}\",\"pp\":\"{pp}\",\"to\":\"{to}\"}}";

            var envelope = Deserialize<Message>(target, json, deserializeMethod);

            var message = envelope.ShouldBeOfType<Message>();
            Assert.AreEqual(type, message.Type);

            var content = message.Content.ShouldBeOfType<PlainDocument>();
            Assert.AreEqual(text, content.Value);
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_UnknownJsonContentMessage_ReturnsValidInstance([Values(DeserializeMethod.String, DeserializeMethod.Stream)] DeserializeMethod deserializeMethod)
        {
            var target = GetTarget();

            var id = EnvelopeId.NewId();
            var from = Dummy.CreateNode();
            var pp = Dummy.CreateNode();
            var to = Dummy.CreateNode();

            var type = Dummy.CreateJsonMediaType();

            var propertyName1 = Dummy.CreateRandomStringExtended(10);
            var propertyName2 = Dummy.CreateRandomStringExtended(10);
            var propertyValue1 = Dummy.CreateRandomStringExtended(10);
            var propertyValue2 = (long)Dummy.CreateRandomInt(1000);

            string json =
                $"{{\"type\":\"{type}\",\"content\":{{\"{propertyName1.Escape()}\":\"{propertyValue1.Escape()}\",\"{propertyName2.Escape()}\":{propertyValue2}}},\"id\":\"{id}\",\"from\":\"{@from}\",\"pp\":\"{pp}\",\"to\":\"{to}\"}}";

            var envelope = Deserialize<Message>(target, json, deserializeMethod);

            var message = envelope.ShouldBeOfType<Message>();
            Assert.AreEqual(type, message.Type);

            var content = message.Content.ShouldBeOfType<JsonDocument>();
            Assert.AreEqual(content[propertyName1], propertyValue1);
            Assert.AreEqual(content[propertyName2], propertyValue2);
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_IdentityDocumentMessage_ReturnsValidInstance([Values(DeserializeMethod.String, DeserializeMethod.Stream)] DeserializeMethod deserializeMethod)
        {
            var target = GetTarget();

            var id = EnvelopeId.NewId();
            var from = Dummy.CreateNode();
            var to = Dummy.CreateNode();

            var identityDocument = Dummy.CreateIdentityDocument();
            var type = identityDocument.GetMediaType();

            string json =
                $"{{\"type\":\"{type}\",\"content\":\"{identityDocument}\",\"id\":\"{id}\",\"from\":\"{@from}\",\"to\":\"{to}\"}}";

            var envelope = Deserialize<Message>(target, json, deserializeMethod);

            var message = envelope.ShouldBeOfType<Message>();
            Assert.AreEqual(type, message.Type);
            var content = message.Content.ShouldBeOfType<IdentityDocument>();
            Assert.AreEqual(identityDocument.Value, content.Value);
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_ChatStateMessage_ReturnsValidInstance([Values(DeserializeMethod.String, DeserializeMethod.Stream)] DeserializeMethod deserializeMethod)
        {
            var target = GetTarget();

            var id = EnvelopeId.NewId();
            var from = Dummy.CreateNode();
            var pp = Dummy.CreateNode();
            var to = Dummy.CreateNode();
            var state = ChatStateEvent.Deleting;

            string json =
                $"{{\"type\":\"application/vnd.lime.chatstate+json\",\"content\":{{\"state\":\"{state.ToString().ToLowerInvariant()}\"}},\"id\":\"{id}\",\"from\":\"{@from}\",\"pp\":\"{pp}\",\"to\":\"{to}\"}}";

            var envelope = Deserialize<Message>(target, json, deserializeMethod);

            var message = envelope.ShouldBeOfType<Message>();
            var chatState = message.Content.ShouldBeOfType<ChatState>();
            Assert.AreEqual(state, chatState.State);
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_CapabilityRequestCommand_ReturnsValidInstance([Values(DeserializeMethod.String, DeserializeMethod.Stream)] DeserializeMethod deserializeMethod)
        {
            var target = GetTarget();

            var contentType1 = Dummy.CreateJsonMediaType();
            var contentType2 = Dummy.CreateJsonMediaType();
            var resourceType1 = Dummy.CreateJsonMediaType();
            var resourceType2 = Dummy.CreateJsonMediaType();

            var method = CommandMethod.Get;
            var id = EnvelopeId.NewId();
            var from = Dummy.CreateNode();
            var to = Dummy.CreateNode();
            var resourceUri = new LimeUri("/capability");

            string json =
                $"{{\"uri\":\"{resourceUri}\",\"type\":\"application/vnd.lime.capability+json\",\"resource\":{{\"contentTypes\":[\"{contentType1}\",\"{contentType2}\"],\"resourceTypes\":[\"{resourceType1}\",\"{resourceType2}\"]}},\"method\":\"{method.ToString().ToCamelCase()}\",\"id\":\"{id}\",\"from\":\"{@from}\",\"to\":\"{to}\"}}";

            var envelope = Deserialize<Command>(target, json, deserializeMethod);

            var command = envelope.ShouldBeOfType<Command>();
            Assert.AreEqual(method, command.Method);
            var capability = command.Resource.ShouldBeOfType<Capability>();
            Assert.IsTrue(capability.ContentTypes.Any(c => c.Equals(contentType1)));
            Assert.IsTrue(capability.ContentTypes.Any(c => c.Equals(contentType2)));
            Assert.IsTrue(capability.ResourceTypes.Any(c => c.Equals(resourceType1)));
            Assert.IsTrue(capability.ResourceTypes.Any(c => c.Equals(resourceType2)));
            Assert.AreEqual(command.Uri, resourceUri);
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_AbsoluteUriRequestCommand_ReturnsValidInstance([Values(DeserializeMethod.String, DeserializeMethod.Stream)] DeserializeMethod deserializeMethod)
        {
            var target = GetTarget();

            var method = CommandMethod.Get;
            var id = EnvelopeId.NewId();
            var from = Dummy.CreateNode();
            var pp = Dummy.CreateNode();
            var to = Dummy.CreateNode();
            var randomKey1 = "randomString1";
            var randomKey2 = "randomString2";
            var randomString1 = Dummy.CreateRandomStringExtended(50);
            var randomString2 = Dummy.CreateRandomStringExtended(50);
            var resourceUri = Dummy.CreateAbsoluteLimeUri();

            string json =
                $"{{\"uri\":\"{resourceUri}\",\"method\":\"get\",\"id\":\"{id}\",\"from\":\"{@from}\",\"pp\":\"{pp}\",\"to\":\"{to}\",\"metadata\":{{\"{randomKey1}\":\"{randomString1.Escape()}\",\"{randomKey2}\":\"{randomString2.Escape()}\"}}}}";

            var envelope = Deserialize<Command>(target, json, deserializeMethod);

            var command = envelope.ShouldBeOfType<Command>();
            Assert.AreEqual(method, command.Method);
            Assert.AreEqual(command.Uri, resourceUri);
            Assert.IsNull(command.Type);
            Assert.IsNull(command.Resource);
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_PresenceRequestCommand_ReturnsValidInstance([Values(DeserializeMethod.String, DeserializeMethod.Stream)] DeserializeMethod deserializeMethod)
        {
            var target = GetTarget();

            var method = CommandMethod.Get;
            var id = EnvelopeId.NewId();
            var from = Dummy.CreateNode();
            var to = Dummy.CreateNode();
            var resourceUri = new LimeUri("/presence");
            var status = PresenceStatus.Available;
            var message = Dummy.CreateRandomStringExtended(100);
            var routingRule = RoutingRule.Identity;
            var lastSeen = DateTimeOffset.UtcNow;
            var priority = Dummy.CreateRandomInt(100);

            string json =
                $"{{\"uri\":\"{resourceUri}\",\"type\":\"application/vnd.lime.presence+json\",\"resource\":{{\"status\": \"{status.ToString().ToCamelCase()}\",\"message\":\"{message.Escape()}\",\"routingRule\":\"{routingRule.ToString().ToCamelCase()}\",\"lastSeen\":\"{lastSeen.ToUniversalTime().ToString(StringJsonExtensions.DATE_FORMAT, CultureInfo.InvariantCulture)}\",\"priority\":{priority}}},\"method\":\"{method.ToString().ToCamelCase()}\",\"id\":\"{id}\",\"from\":\"{@from}\",\"to\":\"{to}\"}}";

            var envelope = Deserialize<Command>(target, json, deserializeMethod);

            var command = envelope.ShouldBeOfType<Command>();
            command.Method.ShouldBe(method);
            var presence = command.Resource.ShouldBeOfType<Presence>();
            presence.Status.ShouldBe(status);
            presence.Message.ShouldBe(message);
            presence.RoutingRule.ShouldBe(routingRule);
            presence.LastSeen.ShouldNotBe(null);
            presence.LastSeen.Value.Year.ShouldBe(lastSeen.Year);
            presence.LastSeen.Value.Month.ShouldBe(lastSeen.Month);
            presence.LastSeen.Value.Day.ShouldBe(lastSeen.Day);
            presence.LastSeen.Value.Hour.ShouldBe(lastSeen.Hour);
            presence.LastSeen.Value.Minute.ShouldBe(lastSeen.Minute);
            presence.LastSeen.Value.Second.ShouldBe(lastSeen.Second);
            presence.Priority.ShouldBe(priority);
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_ContactCollectionResponseCommand_ReturnsValidInstance([Values(DeserializeMethod.String, DeserializeMethod.Stream)] DeserializeMethod deserializeMethod)
        {
            var target = GetTarget();

            var identity1 = Dummy.CreateIdentity();
            var name1 = Dummy.CreateRandomStringExtended(50);
            var identity2 = Dummy.CreateIdentity();
            var name2 = Dummy.CreateRandomStringExtended(50);
            var identity3 = Dummy.CreateIdentity();
            var name3 = Dummy.CreateRandomStringExtended(50);

            var id = EnvelopeId.NewId();
            var from = Dummy.CreateNode();
            var to = Dummy.CreateNode();

            string json =
                $"{{\"type\":\"application/vnd.lime.collection+json\",\"resource\":{{\"itemType\":\"application/vnd.lime.contact+json\",\"total\":3,\"items\":[{{\"identity\":\"{identity1}\",\"name\":\"{name1.Escape()}\",\"isPending\":true,\"shareAccountInfo\":false}},{{\"identity\":\"{identity2}\",\"name\":\"{name2.Escape()}\",\"sharePresence\":false}},{{\"identity\":\"{identity3}\",\"name\":\"{name3.Escape()}\",\"isPending\":true,\"sharePresence\":false}}]}},\"method\":\"get\",\"status\":\"success\",\"id\":\"{id}\",\"from\":\"{@from}\",\"to\":\"{to}\"}}";

            var envelope = Deserialize<Command>(target, json, deserializeMethod);

            var command = envelope.ShouldBeOfType<Command>();
            var documents = command.Resource.ShouldBeOfType<DocumentCollection>();
            Assert.AreEqual(3, documents.Items.Length);

            var contacts = documents.Cast<Contact>().ToArray();
            Assert.IsTrue(contacts[0].Identity.Equals(identity1));
            Assert.IsTrue(contacts[0].Name.Equals(name1));
            Assert.IsTrue(contacts[0].IsPending.HasValue && contacts[0].IsPending.Value);
            Assert.IsTrue(contacts[0].ShareAccountInfo.HasValue && !contacts[0].ShareAccountInfo.Value);

            Assert.IsTrue(contacts[1].Identity.Equals(identity2));
            Assert.IsTrue(contacts[1].SharePresence.HasValue && !contacts[1].SharePresence.Value);

            Assert.IsTrue(contacts[2].Identity.Equals(identity3));
            Assert.IsTrue(contacts[2].IsPending.HasValue && contacts[2].IsPending.Value);
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_ReceiptRequestCommand_ReturnsValidInstance([Values(DeserializeMethod.String, DeserializeMethod.Stream)] DeserializeMethod deserializeMethod)
        {
            var target = GetTarget();

            var method = CommandMethod.Set;
            var id = EnvelopeId.NewId();

            string json =
                $"{{\"type\":\"application/vnd.lime.receipt+json\",\"resource\":{{\"events\":[\"dispatched\",\"received\"]}},\"method\":\"{method.ToString().ToCamelCase()}\",\"id\":\"{id}\"}}";

            var envelope = Deserialize<Command>(target, json, deserializeMethod);

            var command = envelope.ShouldBeOfType<Command>();
            Assert.AreEqual(Receipt.MIME_TYPE, command.Type.ToString());
            command.Resource.ShouldBeOfType<Receipt>();
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_FailureCapabilityResponseCommand_ReturnsValidInstance([Values(DeserializeMethod.String, DeserializeMethod.Stream)] DeserializeMethod deserializeMethod)
        {
            var target = GetTarget();

            var method = CommandMethod.Get;
            var status = CommandStatus.Failure;
            var reason = Dummy.CreateReason();
            var id = EnvelopeId.NewId();
            var from = Dummy.CreateNode();
            var to = Dummy.CreateNode();

            string json =
                $"{{\"method\":\"{method.ToString().ToCamelCase()}\",\"id\":\"{id}\",\"from\":\"{@from}\",\"to\":\"{to}\",\"status\":\"{status}\",\"reason\":{{\"code\":{reason.Code},\"description\":\"{reason.Description}\"}}}}";

            var envelope = Deserialize<Command>(target, json, deserializeMethod);

            var command = envelope.ShouldBeOfType<Command>();
            Assert.AreEqual(method, command.Method);
            Assert.IsNotNull(command.Reason);
            Assert.AreEqual(reason.Code, command.Reason.Code);
            Assert.AreEqual(reason.Description, command.Reason.Description);
            Assert.IsNull(command.Type);
            Assert.IsNull(command.Resource);
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_ReceivedNotification_ReturnsValidInstance([Values(DeserializeMethod.String, DeserializeMethod.Stream)] DeserializeMethod deserializeMethod)
        {
            var target = GetTarget();

            var id = EnvelopeId.NewId();
            var from = Dummy.CreateNode();
            var pp = Dummy.CreateNode();
            var to = Dummy.CreateNode();
            var @event = Event.Received;

            string json =
                $"{{\"event\":\"{@event.ToString().ToCamelCase()}\",\"id\":\"{id}\",\"from\":\"{@from}\",\"pp\":\"{pp}\",\"to\":\"{to}\"}}";

            var envelope = Deserialize<Notification>(target, json, deserializeMethod);

            var notification = envelope.ShouldBeOfType<Notification>();
            Assert.AreEqual(id, notification.Id);
            Assert.AreEqual(@event, notification.Event);
            Assert.IsNull(notification.Reason);
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_FailedNotification_ReturnsValidInstance([Values(DeserializeMethod.String, DeserializeMethod.Stream)] DeserializeMethod deserializeMethod)
        {
            var target = GetTarget();

            var @event = Event.Received;
            var reasonCode = Dummy.CreateRandomInt(100);
            var reasonDescription = Dummy.CreateRandomStringExtended(100);
            var id = EnvelopeId.NewId();
            var from = Dummy.CreateNode();
            var to = Dummy.CreateNode();

            string json =
                $"{{\"event\":\"{@event.ToString().ToCamelCase()}\",\"id\":\"{id}\",\"from\":\"{@from}\",\"to\":\"{to}\",\"reason\":{{\"code\":{reasonCode},\"description\":\"{reasonDescription.Escape()}\"}}}}";

            var envelope = Deserialize<Notification>(target, json, deserializeMethod);

            var notification = envelope.ShouldBeOfType<Notification>();
            Assert.AreEqual(@event, notification.Event);
            Assert.IsNotNull(notification.Reason);
            Assert.AreEqual(reasonCode, notification.Reason.Code);
            Assert.AreEqual(reasonDescription, notification.Reason.Description);
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_AuthenticatingSession_ReturnsValidInstance([Values(DeserializeMethod.String, DeserializeMethod.Stream)] DeserializeMethod deserializeMethod)
        {
            var target = GetTarget();

            var id = EnvelopeId.NewId();
            var from = Dummy.CreateNode();
            var to = Dummy.CreateNode();
            var password = Dummy.CreateRandomString(10).ToBase64();
            var state = SessionState.Authenticating;

            string json =
                $"{{\"state\":\"{state.ToString().ToCamelCase()}\",\"scheme\":\"plain\",\"authentication\":{{\"password\":\"{password}\"}},\"id\":\"{id}\",\"from\":\"{@from}\",\"to\":\"{to}\"}}";

            var envelope = Deserialize<Session>(target, json, deserializeMethod);

            var session = envelope.ShouldBeOfType<Session>();
            Assert.AreEqual(state, session.State);
            var plainAuthentication = session.Authentication.ShouldBeOfType<PlainAuthentication>();
            Assert.AreEqual(password, plainAuthentication.Password);
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_SessionAuthenticatingWithPlainAuthentication_ReturnsValidInstance([Values(DeserializeMethod.String, DeserializeMethod.Stream)] DeserializeMethod deserializeMethod)
        {
            var serializer = GetTarget();
            var json = "{\"state\":\"authenticating\",\"scheme\":\"plain\",\"authentication\":{\"password\":\"Zg==\"},\"id\":\"ec9c196c-da09-43b0-923b-8ec162705c32\",\"from\":\"andre@takenet.com.br/MINELLI-NOTE\"}";

            var envelope = Deserialize<Session>(serializer, json, deserializeMethod);

            var session = envelope.ShouldBeOfType<Session>();
            session.Scheme.ShouldBe(AuthenticationScheme.Plain);
            var plainAuthentication = session.Authentication.ShouldBeOfType<PlainAuthentication>();
            plainAuthentication.Password.ShouldNotBeEmpty();
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_SessionAuthenticatingWithExternalAuthentication_ReturnsValidInstance([Values(DeserializeMethod.String, DeserializeMethod.Stream)] DeserializeMethod deserializeMethod)
        {
            var serializer = GetTarget();
            var json = "{\"state\":\"authenticating\",\"scheme\":\"external\",\"authentication\":{\"token\":\"dFJZMTRXOE03NHBtcmZRNGY3NFo=\",\"issuer\":\"take.net\"},\"id\":\"ec9c196c-da09-43b0-923b-8ec162705c32\",\"from\":\"andre@takenet.com.br/MINELLI-NOTE\"}";

            var envelope = Deserialize<Session>(serializer, json, deserializeMethod);

            var session = envelope.ShouldBeOfType<Session>();
            session.Scheme.ShouldBe(AuthenticationScheme.External);
            var externalAuthentication = session.Authentication.ShouldBeOfType<ExternalAuthentication>();
            externalAuthentication.Token.ShouldBe("dFJZMTRXOE03NHBtcmZRNGY3NFo=");
            externalAuthentication.Issuer.ShouldBe("take.net");
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_SessionAuthenticatingWithGuestAuthentication_ReturnsValidInstance([Values(DeserializeMethod.String, DeserializeMethod.Stream)] DeserializeMethod deserializeMethod)
        {
            var serializer = GetTarget();
            var json = "{\"state\":\"authenticating\",\"scheme\":\"guest\",\"id\":\"feeb88e2-c209-40cd-b8ab-e14aeebe57ab\",\"from\":\"ca6829ff-1ac8-4dad-ad78-c25a3e4f8f7b@takenet.com.br/MINELLI-NOTE\"}";

            var envelope = Deserialize<Session>(serializer, json, deserializeMethod);

            var session = envelope.ShouldBeOfType<Session>();
            session.Scheme.ShouldBe(AuthenticationScheme.Guest);
            session.Authentication.ShouldBeOfType<GuestAuthentication>();
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_FailedSessionNullProperties_ReturnsValidInstance([Values(DeserializeMethod.String, DeserializeMethod.Stream)] DeserializeMethod deserializeMethod)
        {
            var target = GetTarget();

            var id = EnvelopeId.NewId();
            var from = Dummy.CreateNode();
            var to = Dummy.CreateNode();
            var state = SessionState.Authenticating;
            var reasonCode = Dummy.CreateRandomInt(100);
            var reasonDescription = Dummy.CreateRandomStringExtended(100);

            string json =
                $"{{\"state\":\"{state.ToString().ToCamelCase()}\",\"id\":\"{id}\",\"from\":\"{@from}\",\"to\":\"{to}\",\"reason\":{{\"code\":{reasonCode},\"description\":\"{reasonDescription.Escape()}\"}},\"encryptionOptions\":null,\"compressionOptions\":null,\"compression\":null,\"encryption\":null}}";

            var envelope = Deserialize<Session>(target, json, deserializeMethod);

            var session = envelope.ShouldBeOfType<Session>();
            Assert.AreEqual(state, session.State);
            Assert.IsNotNull(session.Reason);
            Assert.AreEqual(reasonCode, session.Reason.Code);
            Assert.AreEqual(reasonDescription, session.Reason.Description);
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_RandomResourceRequestCommand_ReturnsValidInstance([Values(DeserializeMethod.String, DeserializeMethod.Stream)] DeserializeMethod deserializeMethod)
        {
            var target = GetTarget();
            var method = CommandMethod.Set;
            var id = EnvelopeId.NewId();
            DocumentTypeResolver.RegisterDocument<TestDocument>();

            string json =
                $"{{\"type\":\"application/vnd.takenet.testdocument+json\",\"resource\":{{\"double\":10.1, \"NullableDouble\": 10.2, \"Status\":\"Success\"}},\"method\":\"{method.ToString().ToCamelCase()}\",\"id\":\"{id}\"}}";

            var envelope = Deserialize<Command>(target, json, deserializeMethod);

            var command = envelope.ShouldBeOfType<Command>();
            command.Type.ToString().ShouldBe(TestDocument.MIME_TYPE);
            var document = command.Resource.ShouldBeOfType<TestDocument>();
            document.Double.ShouldBe(10.1d);
            document.NullableDouble.ShouldBe(10.2d);
            document.Status.ShouldBe(TestEnum.Success);
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_DocumentContainerDocumentCollectionMessage_ReturnsValidInstance([Values(DeserializeMethod.String, DeserializeMethod.Stream)] DeserializeMethod deserializeMethod)
        {
            var id = EnvelopeId.NewId();
            var json =
                $"{{\"type\":\"application/vnd.lime.collection+json\",\"content\":{{\"total\":4,\"itemType\":\"application/vnd.lime.container+json\",\"items\":[{{\"type\":\"text/plain\",\"value\":\"text1\"}},{{\"type\":\"application/vnd.lime.account+json\",\"value\":{{\"fullName\":\"My Name\",\"photoUri\":\"http://url.com/resource\"}}}},{{\"type\":\"application/q9gn1nsz6y+json\",\"value\":{{\"o4s9txn80q\":\"test\",\"ynpinmi0oq\":20}}}},{{\"type\":\"vxhfxfm3tz/hhnzgm4kmh\",\"value\":\"9nav5pkhswvsw7mh24r1b3agbgic43piylveh1z6xtfz77nibt\"}}]}},\"id\":\"{id}\",\"from\":\"9afudsyl@je29bkh1bs.com/yq1oh\",\"to\":\"9zpfpsuc@d63uusxbfq.com/btp7i\"}}";
            var target = GetTarget();

            var envelope = Deserialize<Message>(target, json, deserializeMethod);

            envelope.ShouldNotBeNull();
            var message = envelope.ShouldBeOfType<Message>();
            var documentCollection = message.Content.ShouldBeOfType<DocumentCollection>();
            documentCollection.Total.ShouldBe(4);
            documentCollection.ItemType.ShouldBe(DocumentContainer.MediaType);
            documentCollection.Items.Length.ShouldBe(4);

            var container1 = documentCollection.Items[0].ShouldBeOfType<DocumentContainer>();
            container1.Type.ShouldBe(PlainText.MediaType);
            var document1 = container1.Value.ShouldBeOfType<PlainText>();
            document1.Text.ShouldBe("text1");

            var container2 = documentCollection.Items[1].ShouldBeOfType<DocumentContainer>();
            container2.Type.ShouldBe(Account.MediaType);
            var document2 = container2.Value.ShouldBeOfType<Account>();
            document2.FullName.ShouldBe("My Name");

            var container3 = documentCollection.Items[2].ShouldBeOfType<DocumentContainer>();
            container3.Type.ShouldBe(MediaType.Parse("application/q9gn1nsz6y+json"));
            var document3 = container3.Value.ShouldBeOfType<JsonDocument>();
            document3.Count.ShouldBeGreaterThan(0);

            var container4 = documentCollection.Items[3].ShouldBeOfType<DocumentContainer>();
            container4.Type.ShouldBe(MediaType.Parse("vxhfxfm3tz/hhnzgm4kmh"));
            var document4 = container4.Value.ShouldBeOfType<PlainDocument>();
            document4.Value.ShouldBe("9nav5pkhswvsw7mh24r1b3agbgic43piylveh1z6xtfz77nibt");
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_DocumentSelect_ReturnValidInstance([Values(DeserializeMethod.String, DeserializeMethod.Stream)] DeserializeMethod deserializeMethod)
        {
            var json = "{\"id\":\"a77fa426-2990-4b98-adbf-db897436017b\",\"to\":\"949839515125748@messenger.gw.msging.net\",\"type\":\"application/vnd.lime.document-select+json\",\"content\":{\"header\":{\"type\":\"text/plain\",\"value\":\"Envie sua localizacao\"},\"options\":[{\"label\":{\"type\":\"application/vnd.lime.input+json\",\"value\":{\"validation\":{\"rule\":\"type\",\"type\":\"application/vnd.lime.location+json\"}}}}]}}";
            var target = GetTarget();

            var envelope = Deserialize<Message>(target, json, deserializeMethod);

            var message = envelope.ShouldBeOfType<Message>();
            var documentSelect = message.Content.ShouldBeOfType<DocumentSelect>();
            var header = documentSelect.Header.Value.ShouldBeOfType<PlainText>();
            header.Text.ShouldBe("Envie sua localizacao");
            documentSelect.Options.Length.ShouldBe(1);
            var input = documentSelect.Options[0].Label.Value.ShouldBeOfType<Input>();
            input.Validation.Rule.ShouldBe(InputValidationRule.Type);
            input.Validation.Type.ShouldBe(Location.MediaType);
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_DocumentSelectMessage_ReturnsValidInstance([Values(DeserializeMethod.String, DeserializeMethod.Stream)] DeserializeMethod deserializeMethod)
        {
            var json = "{\"id\":\"message-id\",\"from\":\"andreb@msging.net\",\"type\":\"application/vnd.lime.document-select+json\",\"content\":{\"header\":{\"type\":\"application/vnd.lime.media-link+json\",\"value\":{\"title\":\"Welcome to Peter's Hats\",\"text\":\"We've got the right hat for everyone.\",\"type\":\"image/jpeg\",\"uri\":\"http://petersapparel.parseapp.com/img/item100-thumb.png\"}},\"options\":[{\"label\":{\"type\":\"application/vnd.lime.web-link+json\",\"value\":{\"text\":\"View Website\",\"uri\":\"https://petersapparel.parseapp.com/view_item?item_id=100\"}}},{\"label\":{\"type\":\"text/plain\",\"value\":\"Start Chatting\"},\"value\":{\"type\":\"application/json\",\"value\":{\"key\":\"key1\",\"value\":1}}}]}}";
            var target = GetTarget();

            var envelope = Deserialize<Message>(target, json, deserializeMethod);

            envelope.ShouldNotBeNull();
            var message = envelope.ShouldBeOfType<Message>();
            var documentSelect = message.Content.ShouldBeOfType<DocumentSelect>();
            var header = documentSelect.Header.Value.ShouldBeOfType<MediaLink>();
            header.Title.ShouldBe("Welcome to Peter's Hats");
            documentSelect.Options.Length.ShouldBe(2);
            var option1Label = documentSelect.Options[0].Label.Value.ShouldBeOfType<WebLink>();
            option1Label.Text.ShouldBe("View Website");
            var option2Label = documentSelect.Options[1].Label.Value.ShouldBeOfType<PlainText>();
            option2Label.Text.ShouldBe("Start Chatting");
            var option2Value = documentSelect.Options[1].Value.Value.ShouldBeOfType<JsonDocument>();
            option2Value.ShouldContainKeyAndValue("key", "key1");
            option2Value.ShouldContainKeyAndValue("value", (long)1);
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_ObserveCommandWithoutId_ReturnsValidInstance([Values(DeserializeMethod.String, DeserializeMethod.Stream)] DeserializeMethod deserializeMethod)
        {
            var json =
                "{\"uri\":\"/sessions/92d8f9fb-0857-4b1a-8f50-f0f37dedc140?expiration=635961514943253147\",\"type\":\"application/vnd.lime.message+json\",\"resource\":{\"id\":\"52e7804c-e483-4f65-85e2-52abc007b35b\",\"from\":\"andreb@msging.net/default\",\"to\":\"joao@msging.net\",\"type\":\"text/plain\",\"content\":\"Banana\",\"metadata\":{\"$internalId\":\"66ec04ec-a278-4251-8ff3-6931bf73e04f\"}},\"method\":\"observe\"}";

            var target = GetTarget();

            var actual = Deserialize<Command>(target, json, deserializeMethod);

            var actualCommand = actual.ShouldBeOfType<Command>();
            actualCommand.Resource.ShouldNotBeNull();
            actualCommand.Id.ShouldBeNull();
            actualCommand.Method.ShouldBe(CommandMethod.Observe);
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_ReplyMessage_ReturnsValidInstance([Values(DeserializeMethod.String, DeserializeMethod.Stream)] DeserializeMethod deserializeMethod)
        {
            var target = GetTarget();

            var id = EnvelopeId.NewId();
            var from = Dummy.CreateNode();
            var pp = Dummy.CreateNode();
            var to = Dummy.CreateNode();
            var text1 = Dummy.CreateRandomStringExtended(50);
            var text2 = Dummy.CreateRandomStringExtended(50);

            string json =
                $"{{\"id\":\"{id}\",\"to\":\"{to}\",\"from\":\"{@from}\",\"pp\":\"{pp}\",\"type\":\"application/vnd.lime.reply+json\",\"metadata\":{{}},\"content\":{{\"replied\":{{\"type\":\"text/plain\",\"value\":\"{text1.Escape()}\"}},\"inReplyTo\":{{\"id\":\"{id}\",\"type\":\"text/plain\",\"value\":\"{text2.Escape()}\"}}}}}}";

            var envelope = Deserialize<Message>(target, json, deserializeMethod);

            var message = envelope.ShouldBeOfType<Message>();
            var reply = message.Content.ShouldBeOfType<Reply>();

            Assert.AreEqual(id, reply.InReplyTo.Id);
            var replyText = reply.Replied.Value.ShouldBeOfType<PlainText>();
            Assert.AreEqual(text1, replyText.Text);
            var replyToText = reply.InReplyTo.Value.ShouldBeOfType<PlainText>();
            Assert.AreEqual(text2, replyToText.Text);
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_CopyAndPasteMessage_ReturnsValidInstance([Values(DeserializeMethod.String, DeserializeMethod.Stream)] DeserializeMethod deserializeMethod)
        {
            var target = GetTarget();

            var id = EnvelopeId.NewId();
            var from = Dummy.CreateNode();
            var pp = Dummy.CreateNode();
            var to = Dummy.CreateNode();

            var title = Dummy.CreateRandomStringExtended(50);
            var body = Dummy.CreateRandomStringExtended(50);
            var footer = Dummy.CreateRandomStringExtended(50);
            var buttonText = Dummy.CreateRandomStringExtended(50);
            var buttonValue = Dummy.CreateRandomStringExtended(50);

            string json =
                $"{{\"type\":\"application/vnd.lime.copy-and-paste+json\",\"content\":{{\"header\":\"{title.Escape()}\",\"body\":\"{body.Escape()}\",\"footer\":\"{footer.Escape()}\",\"button\":{{\"text\":\"{buttonText.Escape()}\",\"value\":\"{buttonValue.Escape()}\"}}}},\"id\":\"{id}\",\"from\":\"{from}\",\"pp\":\"{pp}\",\"to\":\"{to}\"}}";

            var envelope = Deserialize<Message>(target, json, deserializeMethod);

            var message = envelope.ShouldBeOfType<Message>();
            var copyAndPaste = message.Content.ShouldBeOfType<CopyAndPaste>();

            Assert.AreEqual(body, copyAndPaste.Body);
            Assert.AreEqual(footer, copyAndPaste.Footer);
            Assert.AreEqual(title, copyAndPaste.Header);
            Assert.AreEqual(buttonText, copyAndPaste.Button.Text);
            Assert.AreEqual(buttonValue, copyAndPaste.Button.Value);
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_DocumentWithCustomConverter_UsesCustomConverter()
        {
            // Arrange
            var target = GetTarget();
            target.TryAddConverter(new STJDummyJsonConverter1());

            var command = new Command()
            {
                Resource = new STJDummyObject1() { Property = Guid.NewGuid().ToString() },
                Id = EnvelopeId.NewId(),
                From = new Node("limeUser", "limeprotocol.org", null),
                To = new Node("limeUser", "limeprotocol.org", null),
                Method = CommandMethod.Set,
                Uri = new LimeUri("/Dummy")
            };

            // Act - Serialize with standard options and deserialize with our serializer that has a custom converter
            var json = JsonSerializer.Serialize(command, target.Options);
            var deserializedCommand = (Command)target.Deserialize(json);

            // Assert
            var actualResource = deserializedCommand.Resource.ShouldBeOfType<STJDummyObject1>();
            actualResource.Property.ShouldBe(STJDummyJsonConverter1.PropertyDefaultValueConverted);
        }

        #endregion

        #region Converter

        [Test]
        [Category("Converter")]
        public void TryAddConverter_ShouldIgnoreDuplicate()
        {
            var target = GetTarget();

            var firstInsertResult = target.TryAddConverter(new STJDummyJsonConverter1());
            var secondInsertResult = target.TryAddConverter(new STJDummyJsonConverter1());

            firstInsertResult.ShouldBeTrue();
            secondInsertResult.ShouldBeTrue();
            target.Options.Converters
                .Where(c => c.GetType() == typeof(STJDummyJsonConverter1))
                .Count().ShouldBe(2);
        }

        [Test]
        [Category("Converter")]
        public void TryAddConverter_ShouldNotIgnoreDuplicate()
        {
            var target = GetTarget();

            var firstInsertResult = target.TryAddConverter(new STJDummyJsonConverter1(), ignoreDuplicates: false);
            var secondInsertResult = target.TryAddConverter(new STJDummyJsonConverter1(), ignoreDuplicates: false);

            firstInsertResult.ShouldBeTrue();
            secondInsertResult.ShouldBeFalse();
            target.Options.Converters
                .Where(c => c.GetType() == typeof(STJDummyJsonConverter1))
                .Count().ShouldBe(1);
        }

        [Test]
        [Category("Converter")]
        public void TryAddConverter_ShouldHaveDocumentJsonConverterAsTheLast()
        {
            var target = GetTarget();

            target.TryAddConverter(new STJDummyJsonConverter1());
            target.TryAddConverter(new STJDummyJsonConverter2());

            target.Options.Converters.Last().ShouldBeOfType<Lime.Protocol.Serialization.SystemTextJson.Converters.DocumentJsonConverter>();
        }

        #endregion
    }

    // System.Text.Json versions of the dummy objects and converters used in tests
    internal abstract class STJDummyObject : Document
    {
        public STJDummyObject(int n) : base(MediaType.Parse($"application/dummy_stj_object{n}+json")) { }
        public string Property { get; set; }
    }

    internal sealed class STJDummyObject1 : STJDummyObject
    {
        public STJDummyObject1() : base(1) { }
    }

    internal sealed class STJDummyObject2 : STJDummyObject
    {
        public STJDummyObject2() : base(2) { }
    }

    internal sealed class STJDummyJsonConverter1 : JsonConverter<STJDummyObject1>
    {
        public static string PropertyDefaultValueConverted => "STJ Dummy Property value";

        public override STJDummyObject1 Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Skip the JSON value and return a fixed value
            using var doc = System.Text.Json.JsonDocument.ParseValue(ref reader);
            return new STJDummyObject1 { Property = PropertyDefaultValueConverted };
        }

        public override void Write(System.Text.Json.Utf8JsonWriter writer, STJDummyObject1 value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("property", value.Property);
            writer.WriteEndObject();
        }
    }

    internal sealed class STJDummyJsonConverter2 : JsonConverter<STJDummyObject2>
    {
        public override STJDummyObject2 Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(System.Text.Json.Utf8JsonWriter writer, STJDummyObject2 value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
