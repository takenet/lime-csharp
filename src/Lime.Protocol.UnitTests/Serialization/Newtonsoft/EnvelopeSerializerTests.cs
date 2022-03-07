﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Lime.Messaging.Contents;
using Lime.Messaging.Resources;
using Lime.Protocol.Security;
using Lime.Protocol.Serialization;
using Lime.Protocol.Serialization.Newtonsoft;
using Lime.Protocol.UnitTests.Serialization.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Shouldly;
using Lime.Messaging;
using Lime.Protocol.Serialization.Newtonsoft.Converters;

namespace Lime.Protocol.UnitTests.Serialization.Newtonsoft
{
    [TestFixture]
    public class EnvelopeSerializerTests
    {
        public IDocumentTypeResolver DocumentTypeResolver;

        private EnvelopeSerializer GetTarget()
        {
            DocumentTypeResolver = new DocumentTypeResolver().WithMessagingDocuments();
            DocumentTypeResolver.RegisterDocument(typeof(DummyObject1));
            DocumentTypeResolver.RegisterDocument(typeof(DummyObject2));
            return new EnvelopeSerializer(DocumentTypeResolver);
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

        [Test]
        [Category("Serialize")]
        public void Serialize_RelativeUriRequestCommand_ReturnsValidJsonString()
        {
            var target = GetTarget();

            var resource = Dummy.CreateJsonDocument();

            var command = Dummy.CreateCommand(resource);
            command.Pp = Dummy.CreateNode();
            command.Method = CommandMethod.Set;
            command.Uri = Dummy.CreateRelativeLimeUri();

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

        [Test]
        [Category("Serialize")]
        public void Serialize_CapabilityRequestCommand_ReturnsValidJsonString()
        {
            var target = GetTarget();

            var resource = Dummy.CreateCapability();
            var command = Dummy.CreateCommand(resource);
            command.Pp = Dummy.CreateNode();
            command.Method = CommandMethod.Get;


            var metadataKey1 = "randomString1";
            var metadataValue1 = Dummy.CreateRandomString(50);
            var metadataKey2 = "randomString2";
            var metadataValue2 = Dummy.CreateRandomString(50);
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

        [Test]
        [Category("Serialize")]
        public void Serialize_AccountRequestCommand_ReturnsValidJsonString()
        {
            var target = GetTarget();

            var resource = Dummy.CreateAccount();
            var command = Dummy.CreateCommand(resource);
            command.Method = CommandMethod.Get;

            var resultString = target.Serialize(command);

            Assert.IsTrue(resultString.HasValidJsonStackedBrackets());
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.ID_KEY, command.Id));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.FROM_KEY, command.From));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.TO_KEY, command.To));
            Assert.IsTrue(resultString.ContainsJsonProperty(Command.METHOD_KEY, command.Method));
            Assert.IsTrue(resultString.ContainsJsonProperty(Command.TYPE_KEY, command.Resource.GetMediaType()));
            Assert.IsTrue(resultString.ContainsJsonKey(Command.RESOURCE_KEY));
            Assert.IsTrue(resultString.ContainsJsonProperty(Command.METHOD_KEY, command.Method));

            Assert.IsTrue(resultString.ContainsJsonProperty(Account.FULL_NAME_KEY, resource.FullName));
            Assert.IsTrue(resultString.ContainsJsonProperty(Account.PHOTO_URI_KEY, resource.PhotoUri.OriginalString));

            Assert.IsFalse(resultString.ContainsJsonKey(Envelope.PP_KEY));
            Assert.IsFalse(resultString.ContainsJsonKey(Command.METADATA_KEY));
            Assert.IsFalse(resultString.ContainsJsonKey(Command.STATUS_KEY));
            Assert.IsFalse(resultString.ContainsJsonKey(Command.REASON_KEY));
        }

        [Test]
        [Category("Serialize")]
        public void Serialize_RosterResponseCommand_ReturnsValidJsonString()
        {
            var target = GetTarget();

            var resource = Dummy.CreateRoster();
            var command = Dummy.CreateCommand(resource);
            command.Pp = Dummy.CreateNode();
            command.Method = CommandMethod.Get;
            command.Status = CommandStatus.Success;

            var metadataKey1 = "randomString1";
            var metadataValue1 = Dummy.CreateRandomString(50);
            var metadataKey2 = "randomString2";
            var metadataValue2 = Dummy.CreateRandomString(50);
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

            Assert.IsTrue(resultString.ContainsJsonKey(DocumentCollection.ITEMS_KEY));
            Assert.IsTrue(resultString.ContainsJsonKey(DocumentCollection.TOTAL_KEY));
            var contacts = resource.Items.Cast<Contact>().ToArray();
            Assert.IsTrue(resultString.ContainsJsonProperty(Contact.IDENTITY_KEY, contacts[0].Identity));
            Assert.IsTrue(resultString.ContainsJsonProperty(Contact.NAME_KEY, contacts[0].Name));
            Assert.IsTrue(resultString.ContainsJsonProperty(Contact.IS_PENDING_KEY, contacts[0].IsPending));
            Assert.IsTrue(resultString.ContainsJsonProperty(Contact.SHARE_ACCOUNT_INFO_KEY, contacts[0].ShareAccountInfo));
            Assert.IsTrue(resultString.ContainsJsonProperty(Contact.IDENTITY_KEY, contacts[1].Identity));
            Assert.IsTrue(resultString.ContainsJsonProperty(Contact.NAME_KEY, contacts[1].Name));
            Assert.IsTrue(resultString.ContainsJsonProperty(Contact.SHARE_PRESENCE_KEY, contacts[1].SharePresence));
            Assert.IsTrue(resultString.ContainsJsonProperty(Contact.IDENTITY_KEY, contacts[2].Identity));
            Assert.IsTrue(resultString.ContainsJsonProperty(Contact.NAME_KEY, contacts[2].Name));

            Assert.IsTrue(resultString.ContainsJsonKey(Command.STATUS_KEY));
            Assert.IsFalse(resultString.ContainsJsonKey(Command.REASON_KEY));
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

            var resource = Dummy.CreateDocumentCollection<Contact>(
                contact1,
                contact2,
                contact3);

            var command = Dummy.CreateCommand(resource);
            command.Pp = Dummy.CreateNode();
            command.Method = CommandMethod.Get;
            command.Status = CommandStatus.Success;

            var metadataKey1 = "randomString1";
            var metadataValue1 = Dummy.CreateRandomString(50);
            var metadataKey2 = "randomString2";
            var metadataValue2 = Dummy.CreateRandomString(50);
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

            Assert.IsTrue(resultString.ContainsJsonKey(DocumentCollection.ITEMS_KEY));
            Assert.IsTrue(resultString.ContainsJsonProperty(DocumentCollection.ITEM_TYPE_KEY, contact1.GetMediaType()));
            Assert.IsTrue(resultString.ContainsJsonProperty(DocumentCollection.TOTAL_KEY, resource.Items.Length));

            Assert.IsTrue(resultString.ContainsJsonKey(Command.STATUS_KEY));
            Assert.IsFalse(resultString.ContainsJsonKey(Command.REASON_KEY));
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
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.ID_KEY, command.Id));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.FROM_KEY, command.From));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.TO_KEY, command.To));
            Assert.IsTrue(resultString.ContainsJsonProperty(Command.METHOD_KEY, command.Method));
            Assert.IsTrue(resultString.ContainsJsonProperty(Command.TYPE_KEY, command.Resource.GetMediaType()));
            Assert.IsTrue(resultString.ContainsJsonKey(Command.RESOURCE_KEY));
            Assert.IsTrue(resultString.ContainsJsonProperty(Command.METHOD_KEY, command.Method));

            Assert.IsTrue(resultString.ContainsJsonProperty(Presence.MESSAGE_KEY, resource.Message));
            Assert.IsTrue(resultString.ContainsJsonProperty(Presence.LAST_SEEN_KEY, resource.LastSeen));

            Assert.IsFalse(resultString.ContainsJsonKey(Envelope.PP_KEY));
            Assert.IsFalse(resultString.ContainsJsonKey(Command.METADATA_KEY));
            Assert.IsFalse(resultString.ContainsJsonProperty(Command.STATUS_KEY, "pending"));
            Assert.IsFalse(resultString.ContainsJsonKey(Command.REASON_KEY));
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
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.ID_KEY, command.Id));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.FROM_KEY, command.From));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.TO_KEY, command.To));
            Assert.IsTrue(resultString.ContainsJsonProperty(Command.METHOD_KEY, command.Method));
            Assert.IsTrue(resultString.ContainsJsonProperty(Command.TYPE_KEY, command.Resource.GetMediaType()));
            Assert.IsTrue(resultString.ContainsJsonKey(Command.RESOURCE_KEY));
            Assert.IsTrue(resultString.ContainsJsonProperty(Command.METHOD_KEY, command.Method));

            Assert.IsTrue(resultString.ContainsJsonProperty(Presence.MESSAGE_KEY, resource.Message));
            Assert.IsTrue(resultString.ContainsJsonProperty(Presence.LAST_SEEN_KEY, resource.LastSeen));

            Assert.IsFalse(resultString.ContainsJsonKey(Envelope.PP_KEY));
            Assert.IsFalse(resultString.ContainsJsonKey(Command.METADATA_KEY));
            Assert.IsFalse(resultString.ContainsJsonProperty(Command.STATUS_KEY, "pending"));
            Assert.IsFalse(resultString.ContainsJsonKey(Command.REASON_KEY));
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

        [Test]
        [Category("Serialize")]
        public void Serialize_UnknownJsonContentMessage_ReturnsValidJsonString()
        {
            var target = GetTarget();

            var content = Dummy.CreateJsonDocument();
            var message = Dummy.CreateMessage(content);
            message.Pp = Dummy.CreateNode();

            var metadataKey1 = "randomString1";
            var metadataValue1 = Dummy.CreateRandomString(50);
            var metadataKey2 = "randomString2";
            var metadataValue2 = Dummy.CreateRandomString(50);
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
                // TODO: Verify for array properties
                if (!keyValuePair.Value.GetType().IsArray)
                {
                    Assert.IsTrue(resultString.ContainsJsonProperty(keyValuePair.Key, keyValuePair.Value));
                }
            }

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

            var metadataKey1 = "randomString1";
            var metadataValue1 = Dummy.CreateRandomString(50);
            var metadataKey2 = "randomString2";
            var metadataValue2 = Dummy.CreateRandomString(50);
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

            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.FROM_KEY, message.From));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.TO_KEY, message.To));
            Assert.IsTrue(resultString.ContainsJsonProperty(Message.TYPE_KEY, message.Content.GetMediaType()));
            Assert.IsTrue(resultString.ContainsJsonKey(Message.CONTENT_KEY));
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
            var metadataKey2 = "randomString2";
            var metadataValue2 = Dummy.CreateRandomString(50);
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
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.FROM_KEY, notification.From));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.TO_KEY, notification.To));
            Assert.IsTrue(resultString.ContainsJsonProperty(Notification.EVENT_KEY, notification.Event));
            Assert.IsTrue(resultString.ContainsJsonProperty(Reason.CODE_KEY, notification.Reason.Code));
            Assert.IsTrue(resultString.ContainsJsonProperty(Reason.DESCRIPTION_KEY, notification.Reason.Description));

            Assert.IsFalse(resultString.ContainsJsonKey(Envelope.PP_KEY));
            Assert.IsFalse(resultString.ContainsJsonKey(Envelope.METADATA_KEY));
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
            var metadataKey2 = "randomString2";
            var metadataValue2 = Dummy.CreateRandomString(50);
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

        [Test]
        [Category("Serialize")]
        public void Serialize_AuthenticatingSessionExternal_ReturnsValidJsonString()
        {
            var target = GetTarget();

            var session = Dummy.CreateSession();
            var authentication = Dummy.CreateExternalAuthentication();
            session.Authentication = authentication;
            session.State = SessionState.Authenticating;

            var metadataKey1 = "randomString1";
            var metadataValue1 = Dummy.CreateRandomString(50);
            var metadataKey2 = "randomString2";
            var metadataValue2 = Dummy.CreateRandomString(50);
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
            Assert.IsTrue(resultString.ContainsJsonProperty(ExternalAuthentication.TOKEN_KEY, authentication.Token));

            Assert.IsFalse(resultString.ContainsJsonKey(Envelope.PP_KEY));
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

        [Test]
        [Category("Serialize")]
        public void Serialize_ChatStateMessage_ReturnsValidJsonString()
        {
            // Arrange
            var chatState = Dummy.CreateChatState();
            var message = Dummy.CreateMessage(chatState);
            var target = GetTarget();

            // Act
            var resultString = target.Serialize(message);

            // Assert
            Assert.IsTrue(resultString.HasValidJsonStackedBrackets());
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.ID_KEY, message.Id));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.FROM_KEY, message.From));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.TO_KEY, message.To));
            Assert.IsTrue(resultString.ContainsJsonProperty(ChatState.STATE_KEY, chatState.State));
        }

        [Test]
        [Category("Serialize")]
        public void Serialize_SelectMessage_ReturnsValidJsonString()
        {
            // Arrange
            var select = Dummy.CreateSelect();
            var message = Dummy.CreateMessage(select);
            var target = GetTarget();

            // Act
            var resultString = target.Serialize(message);

            // Assert
            Assert.IsTrue(resultString.HasValidJsonStackedBrackets());
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.ID_KEY, message.Id));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.FROM_KEY, message.From));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.TO_KEY, message.To));
            Assert.IsTrue(resultString.ContainsJsonProperty(Select.TEXT_KEY, select.Text));
            Assert.IsTrue(resultString.ContainsJsonKey(Select.OPTIONS_KEY));
            foreach (var option in select.Options)
            {
                Assert.IsTrue(resultString.ContainsJsonProperty(SelectOption.TEXT_KEY, option.Text));
                Assert.IsTrue(resultString.ContainsJsonProperty(SelectOption.TYPE_KEY, option.Type));
                if (option.Type.IsJson)
                {
                    var properties = option.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
                    foreach (var property in properties)
                    {
                        var propertyValue = property.GetValue(option);
                        if (propertyValue == null || propertyValue.Equals(property.PropertyType.GetDefaultValue()))
                            continue;
                        try
                        {
                            Assert.IsTrue(resultString.ContainsJsonProperty(property.Name.ToCamelCase(),
                                propertyValue));
                        }
                        catch (NotSupportedException)
                        {
                            continue;
                        }

                    }
                }
                else
                {
                    Assert.IsTrue(resultString.ContainsJsonProperty(SelectOption.VALUE_KEY, option.Value.ToString()));
                }
            }
        }

        [Test]
        [Category("Serialize")]
        public void Serialize_DocumentSelectMessage_ReturnsValidJsonString()
        {
            // Arrange
            var select = Dummy.CreateDocumentSelect();
            var message = Dummy.CreateMessage(select);
            var target = GetTarget();

            // Act
            var resultString = target.Serialize(message);

            // Assert
            var dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(resultString, target.Settings);
            var selectJson = dictionary[Message.CONTENT_KEY].ShouldBeAssignableTo<JObject>();
            Assert.IsTrue(resultString.HasValidJsonStackedBrackets());
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.ID_KEY, message.Id));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.FROM_KEY, message.From));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.TO_KEY, message.To));
            Assert.IsTrue(resultString.ContainsJsonKey(DocumentSelect.HEADER_KEY));
            var headerJson = selectJson[DocumentSelect.HEADER_KEY].ShouldBeAssignableTo<JObject>();
            Assert.IsTrue(resultString.ContainsJsonKey(DocumentSelect.OPTIONS_KEY));
            var optionsJson = selectJson[DocumentSelect.OPTIONS_KEY].ShouldBeAssignableTo<JArray>();
            optionsJson.Count.ShouldBe(select.Options.Length);
        }

        [Test]
        [Category("Serialize")]
        public void Serialize_InputMessage_ReturnsValidJsonString()
        {
            // Arrange
            var input = Dummy.CreateInput();
            var message = Dummy.CreateMessage(input);
            var target = GetTarget();

            // Act
            var resultString = target.Serialize(message);

            // Assert
            Assert.IsTrue(resultString.HasValidJsonStackedBrackets());
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.ID_KEY, message.Id));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.FROM_KEY, message.From));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.TO_KEY, message.To));
        }

        [Test]
        [Category("Serialize")]
        public void Serialize_IdentityDocumentMessage_ReturnsValidJsonString()
        {
            // Arrange
            var identityDocument = Dummy.CreateIdentityDocument();
            var message = Dummy.CreateMessage(identityDocument);
            var target = GetTarget();

            // Act
            var resultString = target.Serialize(message);

            // Assert
            Assert.IsTrue(resultString.HasValidJsonStackedBrackets());
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.ID_KEY, message.Id));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.FROM_KEY, message.From));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.TO_KEY, message.To));
            Assert.IsTrue(resultString.ContainsJsonProperty(Message.TYPE_KEY, message.Content.GetMediaType()));
            Assert.IsTrue(resultString.ContainsJsonKey(Message.CONTENT_KEY));
            Assert.IsTrue(resultString.ContainsJsonProperty(Message.CONTENT_KEY, identityDocument.Value));
        }

        [Test]
        [Category("Serialize")]
        public void Serialize_DocumentContainerDocumentCollectionMessage_ReturnsValidJsonString()
        {
            // Arrange
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

            // Act
            var resultString = target.Serialize(message);

            // Assert
            Assert.IsTrue(resultString.HasValidJsonStackedBrackets());
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.ID_KEY, message.Id));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.FROM_KEY, message.From));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.TO_KEY, message.To));
            Assert.IsTrue(resultString.ContainsJsonProperty(DocumentContainer.VALUE_KEY, document1.Text));
            Assert.IsTrue(resultString.ContainsJsonProperty(Presence.MESSAGE_KEY, document2.Message));
            Assert.IsTrue(resultString.ContainsJsonProperty(Presence.LAST_SEEN_KEY, document2.LastSeen));
            foreach (var keyValuePair in document3)
            {
                // TODO: Verify for array properties
                if (!keyValuePair.Value.GetType().IsArray)
                {
                    Assert.IsTrue(resultString.ContainsJsonProperty(keyValuePair.Key, keyValuePair.Value));
                }
            }
            Assert.IsTrue(resultString.ContainsJsonProperty(DocumentContainer.VALUE_KEY, container4.Value));
        }

        [Test]
        [Category("Serialize")]
        public void Serialize_WebLinkMessage_ReturnsValidJsonString()
        {
            // Arrange
            var uri = new Uri("http://fake.domain.com:5678/file name.jpg");
            var webLink = Dummy.CreateWebLink(uri);
            var message = Dummy.CreateMessage(webLink);
            var target = GetTarget();

            // Act
            var resultString = target.Serialize(message);

            // Assert
            Assert.IsTrue(resultString.HasValidJsonStackedBrackets());
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.ID_KEY, message.Id));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.FROM_KEY, message.From));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.TO_KEY, message.To));
            Assert.IsTrue(resultString.ContainsJsonProperty(Message.TYPE_KEY, message.Content.GetMediaType()));
            Assert.IsTrue(resultString.ContainsJsonKey(Message.CONTENT_KEY));
            Assert.IsTrue(resultString.ContainsJsonProperty(Link.URI_KEY, uri.OriginalString));
            Assert.IsTrue(resultString.ContainsJsonProperty(Link.PREVIEW_TYPE_KEY, webLink.PreviewType));
            Assert.IsTrue(resultString.ContainsJsonProperty(Link.PREVIEW_URI_KEY, webLink.PreviewUri.OriginalString));
        }

        [Test]
        [Category("Serialize")]
        public void Serialize_WebLinkWithEscapedUriMessage_ReturnsValidJsonString()
        {
            // Arrange
            var uri = new Uri("http://fake.domain.com:5678?email=anyone%40gmail.com&address=https%3A%2F%2Fgoogle.com%3Fq%3Dbanana%2520azul");
            var webLink = Dummy.CreateWebLink(uri);
            var message = Dummy.CreateMessage(webLink);
            var target = GetTarget();

            // Act
            var resultString = target.Serialize(message);

            // Assert
            Assert.IsTrue(resultString.HasValidJsonStackedBrackets());
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.ID_KEY, message.Id));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.FROM_KEY, message.From));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.TO_KEY, message.To));
            Assert.IsTrue(resultString.ContainsJsonProperty(Message.TYPE_KEY, message.Content.GetMediaType()));
            Assert.IsTrue(resultString.ContainsJsonKey(Message.CONTENT_KEY));
            Assert.IsTrue(resultString.ContainsJsonProperty(Link.URI_KEY, uri.OriginalString));
            Assert.IsTrue(resultString.ContainsJsonProperty(Link.PREVIEW_TYPE_KEY, webLink.PreviewType));
            Assert.IsTrue(resultString.ContainsJsonProperty(Link.PREVIEW_URI_KEY, webLink.PreviewUri.OriginalString));
        }

        [Test]
        [Category("Serialize")]
        public void Serialize_RandomDocument_ReturnsValidJsonString()
        {
            // Arrange
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

            // Act
            var resultString = target.Serialize(command);

            // Assert
            Assert.IsTrue(resultString.HasValidJsonStackedBrackets());
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.ID_KEY, command.Id));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.FROM_KEY, command.From));
            Assert.IsTrue(resultString.ContainsJsonProperty(Envelope.TO_KEY, command.To));
            Assert.IsTrue(resultString.ContainsJsonProperty(Command.METHOD_KEY, command.Method));
            Assert.IsTrue(resultString.ContainsJsonProperty(Command.TYPE_KEY, command.Resource.GetMediaType()));
            Assert.IsTrue(resultString.ContainsJsonKey(Command.RESOURCE_KEY));
            Assert.IsTrue(resultString.ContainsJsonProperty(Command.METHOD_KEY, command.Method));

            Assert.IsTrue(resultString.ContainsJsonProperty("Status", "Success"));
            Assert.IsTrue(resultString.ContainsJsonProperty("date", "2022-01-28T01:02:03.000Z"));
            Assert.IsTrue(resultString.ContainsJsonProperty("double", 44.9));
            Assert.IsTrue(resultString.ContainsJsonProperty("Address", resource.Address.ToString()));
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
        }

        [Test]
        [Category("Serialize")]
        public void Serialize_DocumentWithCustomConverter()
        {
            // Arrange
            var target = GetTarget();
            target.TryAddConverter(new DummyJsonConverter1());
            target.TryAddConverter(new DummyJsonConverter2());

            var command = new Command()
            {
                Resource = new DummyObject1() { Property = Guid.NewGuid().ToString() },
                Id = EnvelopeId.NewId(),
                From = new Node("limeUser", "limeprotocol.org", null),
                To = new Node("limeUser", "limeprotocol.org", null),
                Method = CommandMethod.Set,
                Uri = new LimeUri("/Dummy")
            };

            // Act
            var json = JsonConvert.SerializeObject(command);
            var deserializedCommand = (Command)target.Deserialize(json);

            // Assert
            var actualMessage = deserializedCommand.Resource.ShouldBeOfType<DummyObject1>();
            actualMessage.Property.ShouldBe(DummyJsonConverter1.PropertyDefaultValueConverted);
        }

        [Test]
        [Category("Serialize")]
        public void Serialize_DocumentWithDefaultConverterAfterRegisterAnyCostomConverters()
        {
            // Arrange
            var target = GetTarget();
            target.TryAddConverter(new DummyJsonConverter1());

            var guid = Guid.NewGuid().ToString();
            var command = new Command()
            {
                Resource = new DummyObject2() { Property = guid },
                Id = EnvelopeId.NewId(),
                From = new Node("limeUser", "limeprotocol.org", null),
                To = new Node("limeUser", "limeprotocol.org", null),
                Method = CommandMethod.Set,
                Uri = new LimeUri("/Dummy")
            };

            // Act
            var json = JsonConvert.SerializeObject(command);
            var deserializedCommand = (Command)target.Deserialize(json);

            // Assert
            var actualMessage = deserializedCommand.Resource.ShouldBeOfType<DummyObject2>();
            actualMessage.Property.ShouldNotBe(DummyJsonConverter1.PropertyDefaultValueConverted);
            actualMessage.Property.ShouldNotBe(guid);
        }
        #endregion

        #region Deserialize

        [Test]
        [Category("Deserialize")]
        public void Deserialize_CapabilityRequestCommand_ReturnsValidInstance()
        {
            var target = GetTarget();

            var contentType1 = Dummy.CreateJsonMediaType();
            var contentType2 = Dummy.CreateJsonMediaType();
            var contentType3 = Dummy.CreateJsonMediaType();

            var resourceType1 = Dummy.CreateJsonMediaType();
            var resourceType2 = Dummy.CreateJsonMediaType();
            var resourceType3 = Dummy.CreateJsonMediaType();

            var method = CommandMethod.Get;

            var id = EnvelopeId.NewId();

            var from = Dummy.CreateNode();
            var pp = Dummy.CreateNode();
            var to = Dummy.CreateNode();

            string randomKey1 = "randomString1";
            string randomKey2 = "randomString2";
            string randomString1 = Dummy.CreateRandomStringExtended(50);
            string randomString2 = Dummy.CreateRandomStringExtended(50);

            var resourceUri = new LimeUri("/capability");

            string json =
                $"{{\"uri\":\"{resourceUri}\",\"type\":\"application/vnd.lime.capability+json\",\"resource\":{{\"contentTypes\":[\"{contentType1}\",\"{contentType2}\",\"{contentType3}\"],\"resourceTypes\":[\"{resourceType1}\",\"{resourceType2}\",\"{resourceType3}\"]}},\"method\":\"{method.ToString().ToCamelCase()}\",\"id\":\"{id}\",\"from\":\"{@from}\",\"pp\":\"{pp}\",\"to\":\"{to}\",\"metadata\":{{\"{randomKey1}\":\"{randomString1.Escape()}\",\"{randomKey2}\":\"{randomString2.Escape()}\"}}}}";

            var envelope = target.Deserialize(json);

            var command = envelope.ShouldBeOfType<Command>();
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

            var capability = command.Resource.ShouldBeOfType<Capability>();
            Assert.IsTrue(capability.ContentTypes.Any(c => c.Equals(contentType1)));
            Assert.IsTrue(capability.ContentTypes.Any(c => c.Equals(contentType2)));
            Assert.IsTrue(capability.ContentTypes.Any(c => c.Equals(contentType3)));

            Assert.IsTrue(capability.ResourceTypes.Any(c => c.Equals(resourceType1)));
            Assert.IsTrue(capability.ResourceTypes.Any(c => c.Equals(resourceType2)));
            Assert.IsTrue(capability.ResourceTypes.Any(c => c.Equals(resourceType3)));

            Assert.IsNotNull(command.Uri);
            Assert.AreEqual(command.Uri, resourceUri);
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_AccountRequestCommand_ReturnsValidInstance()
        {
            var target = GetTarget();
            var method = CommandMethod.Get;
            var id = EnvelopeId.NewId();
            var from = Dummy.CreateNode();
            var to = Dummy.CreateNode();
            var resourceUri = new LimeUri("/account");
            var fullName = Dummy.CreateRandomStringExtended(25);
            var photoUri = Dummy.CreateUri();
            string json =
                $"{{\"uri\":\"{resourceUri}\",\"type\":\"application/vnd.lime.account+json\",\"resource\":{{\"fullName\": \"{fullName.Escape()}\", \"photoUri\": \"{photoUri}\"}},\"method\":\"{method.ToString().ToCamelCase()}\",\"id\":\"{id}\",\"from\":\"{@from}\",\"to\":\"{to}\"}}";

            var envelope = target.Deserialize(json);

            var command = envelope.ShouldBeOfType<Command>();
            command.Id.ShouldBe(id);
            command.From.ShouldBe(from);
            command.To.ShouldBe(to);
            command.Pp.ShouldBe(null);
            command.Metadata.ShouldBe(null);
            command.Method.ShouldBe(method);
            command.Uri.ShouldBe(resourceUri);
            var account = command.Resource.ShouldBeOfType<Account>();
            account.PhotoUri.ShouldBe(photoUri);
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_PresenceRequestCommand_ReturnsValidInstance()
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

            var envelope = target.Deserialize(json);

            var command = envelope.ShouldBeOfType<Command>();
            command.Id.ShouldBe(id);
            command.From.ShouldBe(from);
            command.To.ShouldBe(to);
            command.Pp.ShouldBe(null);
            command.Metadata.ShouldBe(null);

            command.Method.ShouldBe(method);
            command.Uri.ShouldBe(resourceUri);

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
            presence.LastSeen.Value.Millisecond.ShouldBe(lastSeen.Millisecond);
            presence.Priority.ShouldBe(priority);
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_AbsoluteUriRequestCommand_ReturnsValidInstance()
        {
            var target = GetTarget();

            var method = CommandMethod.Get;

            var id = EnvelopeId.NewId();

            var from = Dummy.CreateNode();
            var pp = Dummy.CreateNode();
            var to = Dummy.CreateNode();

            string randomKey1 = "randomString1";
            string randomKey2 = "randomString2";
            string randomString1 = Dummy.CreateRandomStringExtended(50);
            string randomString2 = Dummy.CreateRandomStringExtended(50);

            var resourceUri = Dummy.CreateAbsoluteLimeUri();

            string json =
                $"{{\"uri\":\"{resourceUri}\",\"method\":\"get\",\"id\":\"{id}\",\"from\":\"{@from}\",\"pp\":\"{pp}\",\"to\":\"{to}\",\"metadata\":{{\"{randomKey1}\":\"{randomString1.Escape()}\",\"{randomKey2}\":\"{randomString2.Escape()}\"}}}}";

            var envelope = target.Deserialize(json);

            var command = envelope.ShouldBeOfType<Command>();
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

        [Test]
        [Category("Deserialize")]
        public void Deserialize_ReceiptRequestCommand_ReturnsValidInstance()
        {
            var target = GetTarget();

            var method = CommandMethod.Set;
            var id = EnvelopeId.NewId();

            string json =
                $"{{\"type\":\"application/vnd.lime.receipt+json\",\"resource\":{{\"events\":[\"dispatched\",\"received\"]}},\"method\":\"{method.ToString().ToCamelCase()}\",\"id\":\"{id}\"}}";

            var envelope = target.Deserialize(json);

            var command = envelope.ShouldBeOfType<Command>();
            Assert.AreEqual(id, command.Id);
            Assert.IsNull(command.From);
            Assert.IsNull(command.Pp);
            Assert.IsNull(command.To);

            Assert.AreEqual(method, command.Method);
            Assert.IsNull(command.Metadata);
            Assert.AreEqual(command.Type.ToString(), Receipt.MIME_TYPE);
            Assert.IsNotNull(command.Resource);
            command.Resource.ShouldBeOfType<Receipt>();
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_ContactCollectionResponseCommand_ReturnsValidInstance()
        {
            var target = GetTarget();

            var identity1 = Dummy.CreateIdentity();
            var name1 = Dummy.CreateRandomStringExtended(50);
            var identity2 = Dummy.CreateIdentity();
            var name2 = Dummy.CreateRandomStringExtended(50);
            var identity3 = Dummy.CreateIdentity();
            var name3 = Dummy.CreateRandomStringExtended(50);

            var method = CommandMethod.Get;

            var id = EnvelopeId.NewId();

            var from = Dummy.CreateNode();
            var pp = Dummy.CreateNode();
            var to = Dummy.CreateNode();

            string randomKey1 = "randomString1";
            string randomKey2 = "randomString2";
            string randomString1 = Dummy.CreateRandomStringExtended(50);
            string randomString2 = Dummy.CreateRandomStringExtended(50);

            string json =
                $"{{\"type\":\"application/vnd.lime.collection+json\",\"resource\":{{\"itemType\":\"application/vnd.lime.contact+json\",\"total\":3,\"items\":[{{\"identity\":\"{identity1}\",\"name\":\"{name1.Escape()}\",\"isPending\":true,\"shareAccountInfo\":false}},{{\"identity\":\"{identity2}\",\"name\":\"{name2.Escape()}\",\"sharePresence\":false}},{{\"identity\":\"{identity3}\",\"name\":\"{name3.Escape()}\",\"isPending\":true,\"sharePresence\":false}}]}},\"method\":\"get\",\"status\":\"success\",\"id\":\"{id}\",\"from\":\"{@from}\",\"pp\":\"{pp}\",\"to\":\"{to}\",\"metadata\":{{\"{randomKey1}\":\"{randomString1.Escape()}\",\"{randomKey2}\":\"{randomString2.Escape()}\"}}}}";

            var envelope = target.Deserialize(json);

            var command = envelope.ShouldBeOfType<Command>();
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

            var documents = command.Resource.ShouldBeOfType<DocumentCollection>();
            Assert.IsNotNull(documents.Items);
            Assert.AreEqual(documents.Items.Length, 3);

            var contacts = documents.Cast<Contact>().ToArray();

            Assert.IsTrue(contacts[0].Identity.Equals(identity1));
            Assert.IsTrue(contacts[0].Name.Equals(name1));
            Assert.IsTrue(contacts[0].IsPending.HasValue);
            Assert.IsTrue(contacts[0].IsPending.Value);
            Assert.IsTrue(contacts[0].ShareAccountInfo.HasValue);
            Assert.IsFalse(contacts[0].ShareAccountInfo.Value);
            Assert.IsFalse(contacts[0].SharePresence.HasValue);

            Assert.IsTrue(contacts[1].Identity.Equals(identity2));
            Assert.IsTrue(contacts[1].Name.Equals(name2));
            Assert.IsFalse(contacts[1].IsPending.HasValue);
            Assert.IsFalse(contacts[1].ShareAccountInfo.HasValue);
            Assert.IsTrue(contacts[1].SharePresence.HasValue);
            Assert.IsFalse(contacts[1].SharePresence.Value);

            Assert.IsTrue(contacts[2].Identity.Equals(identity3));
            Assert.IsTrue(contacts[2].Name.Equals(name3));
            Assert.IsTrue(contacts[2].IsPending.HasValue);
            Assert.IsTrue(contacts[2].IsPending.Value);
            Assert.IsFalse(contacts[2].ShareAccountInfo.HasValue);
            Assert.IsTrue(contacts[2].SharePresence.HasValue);
            Assert.IsFalse(contacts[2].SharePresence.Value);
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_ContactCollectionResponseCommand_ReturnsValidInstanceWithCreationDate()
        {
            var target = GetTarget();

            var identity1 = Dummy.CreateIdentity();
            var name1 = Dummy.CreateRandomStringExtended(50);
            var identity2 = Dummy.CreateIdentity();
            var name2 = Dummy.CreateRandomStringExtended(50);
            var identity3 = Dummy.CreateIdentity();
            var name3 = Dummy.CreateRandomStringExtended(50);

            var method = CommandMethod.Get;

            var id = EnvelopeId.NewId();

            var from = Dummy.CreateNode();
            var pp = Dummy.CreateNode();
            var to = Dummy.CreateNode();

            string randomKey1 = "randomString1";
            string randomKey2 = "randomString2";
            string randomString1 = Dummy.CreateRandomStringExtended(50);
            string randomString2 = Dummy.CreateRandomStringExtended(50);

            DateTimeOffset creationDate1 = new DateTime(2019, 03, 15);
            DateTimeOffset creationDate2 = new DateTime(2019, 04, 15);

            string creationDateTime1 = creationDate1.ToString("o");
            string creationDateTime2 = creationDate2.ToString("o");

            string json =
                $"{{\"type\":\"application/vnd.lime.collection+json\",\"resource\":{{\"itemType\":\"application/vnd.lime.contact+json\",\"total\":3,\"items\":[{{\"identity\":\"{identity1}\",\"name\":\"{name1.Escape()}\",\"creationDate\":\"{creationDateTime1}\",\"isPending\":true,\"shareAccountInfo\":false}},{{\"identity\":\"{identity2}\",\"name\":\"{name2.Escape()}\",\"creationDate\":\"{creationDateTime2}\",\"sharePresence\":false}},{{\"identity\":\"{identity3}\",\"name\":\"{name3.Escape()}\",\"isPending\":true,\"sharePresence\":false}}]}},\"method\":\"get\",\"status\":\"success\",\"id\":\"{id}\",\"from\":\"{@from}\",\"pp\":\"{pp}\",\"to\":\"{to}\",\"metadata\":{{\"{randomKey1}\":\"{randomString1.Escape()}\",\"{randomKey2}\":\"{randomString2.Escape()}\"}}}}";

            var envelope = target.Deserialize(json);

            var command = envelope.ShouldBeOfType<Command>();
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

            var documents = command.Resource.ShouldBeOfType<DocumentCollection>();
            Assert.IsNotNull(documents.Items);
            Assert.AreEqual(documents.Items.Length, 3);

            var contacts = documents.Cast<Contact>().ToArray();

            Assert.IsTrue(contacts[0].Identity.Equals(identity1));
            Assert.IsTrue(contacts[0].Name.Equals(name1));
            Assert.IsTrue(contacts[0].IsPending.HasValue);
            Assert.IsTrue(contacts[0].IsPending.Value);
            Assert.IsTrue(contacts[0].ShareAccountInfo.HasValue);
            Assert.IsFalse(contacts[0].ShareAccountInfo.Value);
            Assert.IsFalse(contacts[0].SharePresence.HasValue);

            Assert.IsTrue(contacts[1].Identity.Equals(identity2));
            Assert.IsTrue(contacts[1].Name.Equals(name2));
            Assert.IsFalse(contacts[1].IsPending.HasValue);
            Assert.IsFalse(contacts[1].ShareAccountInfo.HasValue);
            Assert.IsTrue(contacts[1].SharePresence.HasValue);
            Assert.IsFalse(contacts[1].SharePresence.Value);

            Assert.IsTrue(contacts[2].Identity.Equals(identity3));
            Assert.IsTrue(contacts[2].Name.Equals(name3));
            Assert.IsTrue(contacts[2].IsPending.HasValue);
            Assert.IsTrue(contacts[2].IsPending.Value);
            Assert.IsFalse(contacts[2].ShareAccountInfo.HasValue);
            Assert.IsTrue(contacts[2].SharePresence.HasValue);
            Assert.IsFalse(contacts[2].SharePresence.Value);

            Assert.IsTrue(contacts[0].CreationDate.HasValue);
            Assert.IsTrue(contacts[1].CreationDate.HasValue);
            Assert.IsTrue(contacts[0].CreationDate.Equals(creationDate1));
            Assert.IsTrue(contacts[1].CreationDate.Equals(creationDate2));

            Assert.IsFalse(contacts[2].CreationDate.HasValue);
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_FailureCapabilityResponseCommand_ReturnsValidInstance()
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

            var envelope = target.Deserialize(json);
            var command = envelope.ShouldBeOfType<Command>();
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

        [Test]
        [Category("Deserialize")]
        public void Deserialize_TextMessage_ReturnsValidInstance()
        {
            var target = GetTarget();

            var id = EnvelopeId.NewId();
            var from = Dummy.CreateNode();
            var pp = Dummy.CreateNode();
            var to = Dummy.CreateNode();

            string randomKey1 = "randomString1";
            string randomKey2 = "randomString2";
            string randomString1 = Dummy.CreateRandomStringExtended(50);
            string randomString2 = Dummy.CreateRandomStringExtended(50);

            var text = Dummy.CreateRandomStringExtended(50);

            string json =
                $"{{\"type\":\"text/plain\",\"content\":\"{text.Escape()}\",\"id\":\"{id}\",\"from\":\"{@from}\",\"pp\":\"{pp}\",\"to\":\"{to}\",\"metadata\":{{\"{randomKey1}\":\"{randomString1.Escape()}\",\"{randomKey2}\":\"{randomString2.Escape()}\"}}}}";

            var envelope = target.Deserialize(json);

            var message = envelope.ShouldBeOfType<Message>();
            Assert.AreEqual(id, message.Id);
            Assert.AreEqual(from, message.From);
            Assert.AreEqual(pp, message.Pp);
            Assert.AreEqual(to, message.To);
            Assert.IsNotNull(message.Metadata);
            Assert.IsTrue(message.Metadata.ContainsKey(randomKey1));
            Assert.AreEqual(message.Metadata[randomKey1], randomString1);
            Assert.IsTrue(message.Metadata.ContainsKey(randomKey2));
            Assert.AreEqual(message.Metadata[randomKey2], randomString2);

            message.Content.ShouldBeOfType<PlainText>();

            var textContent = (PlainText)message.Content;
            Assert.AreEqual(text, textContent.Text);
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_TextMessageWithNullFromTo_ReturnsValidInstance()
        {
            var target = GetTarget();

            var id = EnvelopeId.NewId();
            Node from = null;
            Node pp = Dummy.CreateNode();
            Node to = null;

            string randomKey1 = "randomString1";
            string randomKey2 = "randomString2";
            string randomString1 = Dummy.CreateRandomStringExtended(50);
            string randomString2 = Dummy.CreateRandomStringExtended(50);

            var text = Dummy.CreateRandomStringExtended(50);

            string json =
                $"{{\"type\":\"text/plain\",\"content\":\"{text.Escape()}\",\"id\":\"{id}\",\"from\":null,\"pp\":\"{pp}\",\"to\":null,\"metadata\":{{\"{randomKey1}\":\"{randomString1.Escape()}\",\"{randomKey2}\":\"{randomString2.Escape()}\"}}}}";

            var envelope = target.Deserialize(json);

            var message = envelope.ShouldBeOfType<Message>();
            Assert.AreEqual(id, message.Id);
            Assert.AreEqual(from, message.From);
            Assert.AreEqual(pp, message.Pp);
            Assert.AreEqual(to, message.To);
            Assert.IsNotNull(message.Metadata);
            Assert.IsTrue(message.Metadata.ContainsKey(randomKey1));
            Assert.AreEqual(message.Metadata[randomKey1], randomString1);
            Assert.IsTrue(message.Metadata.ContainsKey(randomKey2));
            Assert.AreEqual(message.Metadata[randomKey2], randomString2);

            message.Content.ShouldBeOfType<PlainText>();

            var textContent = (PlainText)message.Content;
            Assert.AreEqual(text, textContent.Text);
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_ChatStateMessage_ReturnsValidInstance()
        {
            var target = GetTarget();

            var id = EnvelopeId.NewId();
            var from = Dummy.CreateNode();
            var pp = Dummy.CreateNode();
            var to = Dummy.CreateNode();

            string randomKey1 = "randomString1";
            string randomKey2 = "randomString2";
            string randomString1 = Dummy.CreateRandomStringExtended(50);
            string randomString2 = Dummy.CreateRandomStringExtended(50);

            var state = ChatStateEvent.Deleting;

            string json =
                $"{{\"type\":\"application/vnd.lime.chatstate+json\",\"content\":{{\"state\":\"{state.ToString().ToLowerInvariant()}\"}},\"id\":\"{id}\",\"from\":\"{@from}\",\"pp\":\"{pp}\",\"to\":\"{to}\",\"metadata\":{{\"{randomKey1}\":\"{randomString1.Escape()}\",\"{randomKey2}\":\"{randomString2.Escape()}\"}}}}";

            var envelope = target.Deserialize(json);

            var message = envelope.ShouldBeOfType<Message>();
            Assert.AreEqual(id, message.Id);
            Assert.AreEqual(from, message.From);
            Assert.AreEqual(pp, message.Pp);
            Assert.AreEqual(to, message.To);
            Assert.IsNotNull(message.Metadata);
            Assert.IsTrue(message.Metadata.ContainsKey(randomKey1));
            Assert.AreEqual(message.Metadata[randomKey1], randomString1);
            Assert.IsTrue(message.Metadata.ContainsKey(randomKey2));
            Assert.AreEqual(message.Metadata[randomKey2], randomString2);

            message.Content.ShouldBeOfType<ChatState>();

            var textContent = (ChatState)message.Content;
            Assert.AreEqual(state, textContent.State);
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_DocumentSelect_ReturnValidInstance()
        {
            // Arrange
            var json = "{\"id\":\"a77fa426-2990-4b98-adbf-db897436017b\",\"to\":\"949839515125748@messenger.gw.msging.net\",\"type\":\"application/vnd.lime.document-select+json\",\"content\":{\"header\":{\"type\":\"text/plain\",\"value\":\"Envie sua localizacao\"},\"options\":[{\"label\":{\"type\":\"application/vnd.lime.input+json\",\"value\":{\"validation\":{\"rule\":\"type\",\"type\":\"application/vnd.lime.location+json\"}}}}]}}";
            var target = GetTarget();

            // Act
            var envelope = target.Deserialize(json);

            // Assert
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
        public void Deserialize_UnknownPlainContentMessage_ReturnsValidInstance()
        {
            var target = GetTarget();

            var id = EnvelopeId.NewId();
            var from = Dummy.CreateNode();
            var pp = Dummy.CreateNode();
            var to = Dummy.CreateNode();

            string randomKey1 = "randomString1";
            string randomKey2 = "randomString2";
            string randomString1 = Dummy.CreateRandomStringExtended(50);
            string randomString2 = Dummy.CreateRandomStringExtended(50);

            var type = Dummy.CreatePlainMediaType();
            var text = Dummy.CreateRandomString(50);

            string json =
                $"{{\"type\":\"{type}\",\"content\":\"{text}\",\"id\":\"{id}\",\"from\":\"{@from}\",\"pp\":\"{pp}\",\"to\":\"{to}\",\"metadata\":{{\"{randomKey1}\":\"{randomString1.Escape()}\",\"{randomKey2}\":\"{randomString2.Escape()}\"}}}}";

            var envelope = target.Deserialize(json);

            var message = envelope.ShouldBeOfType<Message>();
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

            var content = message.Content.ShouldBeOfType<PlainDocument>();
            Assert.AreEqual(text, content.Value);

        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_IdentityDocumentMessage_ReturnsValidInstance()
        {
            var target = GetTarget();

            var id = EnvelopeId.NewId();
            var from = Dummy.CreateNode();
            var to = Dummy.CreateNode();

            var identityDocument = Dummy.CreateIdentityDocument();
            var type = identityDocument.GetMediaType();

            string json =
                $"{{\"type\":\"{type}\",\"content\":\"{identityDocument}\",\"id\":\"{id}\",\"from\":\"{@from}\",\"to\":\"{to}\"}}";

            var envelope = target.Deserialize(json);

            var message = envelope.ShouldBeOfType<Message>();
            Assert.AreEqual(id, message.Id);
            Assert.AreEqual(from, message.From);
            Assert.AreEqual(to, message.To);

            Assert.IsNotNull(message.Type);
            Assert.AreEqual(message.Type, type);

            var content = message.Content.ShouldBeOfType<IdentityDocument>();
            Assert.AreEqual(identityDocument.Value, content.Value);

        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_UnknownJsonContentMessage_ReturnsValidInstance()
        {
            var target = GetTarget();

            var id = EnvelopeId.NewId();
            var from = Dummy.CreateNode();
            var pp = Dummy.CreateNode();
            var to = Dummy.CreateNode();

            string randomKey1 = "randomString1";
            string randomKey2 = "randomString2";
            string randomString1 = Dummy.CreateRandomStringExtended(50);
            string randomString2 = Dummy.CreateRandomStringExtended(50);


            var type = Dummy.CreateJsonMediaType();

            var propertyName1 = Dummy.CreateRandomStringExtended(100);
            var propertyName2 = Dummy.CreateRandomStringExtended(100);
            var propertyName3 = Dummy.CreateRandomStringExtended(100);
            var propertyName4 = Dummy.CreateRandomStringExtended(100);

            var arrayPropertyName1 = Dummy.CreateRandomStringExtended(100);
            var arrayPropertyName2 = Dummy.CreateRandomStringExtended(100);
            var arrayPropertyName3 = Dummy.CreateRandomStringExtended(100);
            var arrayPropertyName4 = Dummy.CreateRandomStringExtended(100);
            var arrayPropertyValue1 = Dummy.CreateRandomString(100);
            var arrayPropertyValue2 = (long)Dummy.CreateRandomInt(1000);
            var arrayPropertyValue3 = Dummy.CreateRandomStringExtended(100);
            var arrayPropertyValue4 = false;

            var propertyValue1 = Dummy.CreateRandomStringExtended(100);
            var propertyValue2 = (long)Dummy.CreateRandomInt(1000);
            var propertyValue4 = DateTime.UtcNow;


            string json =
                $"{{\"type\":\"{type}\",\"content\":{{\"{propertyName1.Escape()}\":\"{propertyValue1.Escape()}\",\"{propertyName2.Escape()}\":{propertyValue2},\"{propertyName3.Escape()}\":[{{\"{arrayPropertyName1.Escape()}\":\"{arrayPropertyValue1}\",\"{arrayPropertyName2.Escape()}\":{arrayPropertyValue2}}},{{\"{arrayPropertyName3.Escape()}\":\"{arrayPropertyValue3.Escape()}\",\"{arrayPropertyName4.Escape()}\":{arrayPropertyValue4.ToString().ToLower()}}}],\"{propertyName4.Escape()}\":\"{propertyValue4.ToUniversalTime().ToString(StringJsonExtensions.DATE_FORMAT, CultureInfo.InvariantCulture)}\"}},\"id\":\"{id}\",\"from\":\"{@from}\",\"pp\":\"{pp}\",\"to\":\"{to}\",\"metadata\":{{\"{randomKey1}\":\"{randomString1.Escape()}\",\"{randomKey2}\":\"{randomString2.Escape()}\"}}}}";

            var envelope = target.Deserialize(json);

            var message = envelope.ShouldBeOfType<Message>();
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

            var content = message.Content.ShouldBeOfType<JsonDocument>();

            Assert.IsTrue(content.ContainsKey(propertyName1));
            Assert.AreEqual(content[propertyName1], propertyValue1);
            Assert.IsTrue(content.ContainsKey(propertyName2));
            Assert.AreEqual(content[propertyName2], propertyValue2);
            Assert.IsTrue(content.ContainsKey(propertyName3));
            Assert.IsTrue(content[propertyName3] is IList<object>);

            var list = (IList<object>)content[propertyName3];
            Assert.AreEqual(2, list.Count);

            for (int i = 0; i < list.Count; i++)
            {
                var item = (IDictionary<string, object>)list[i];
                if (i == 0)
                {
                    Assert.IsTrue(item.ContainsKey(arrayPropertyName1));
                    Assert.AreEqual(arrayPropertyValue1, item[arrayPropertyName1]);
                    Assert.IsTrue(item.ContainsKey(arrayPropertyName2));
                    Assert.AreEqual(arrayPropertyValue2, item[arrayPropertyName2]);
                }
                else if (i == 1)
                {
                    Assert.IsTrue(item.ContainsKey(arrayPropertyName3));
                    Assert.AreEqual(arrayPropertyValue3, item[arrayPropertyName3]);
                    Assert.IsTrue(item.ContainsKey(arrayPropertyName4));
                    Assert.AreEqual(arrayPropertyValue4, item[arrayPropertyName4]);
                }
            }

            Assert.IsTrue(content.ContainsKey(propertyName4));

            DateTime dateTime;
            if (content[propertyName4] is DateTime)
            {
                dateTime = (DateTime)content[propertyName4];
            }
            else
            {
                DateTime.TryParse((string)content[propertyName4], null, DateTimeStyles.AdjustToUniversal, out dateTime).ShouldBe(true);
            }
            dateTime.Year.ShouldBe(propertyValue4.Year);
            dateTime.Month.ShouldBe(propertyValue4.Month);
            dateTime.Day.ShouldBe(propertyValue4.Day);
            dateTime.Hour.ShouldBe(propertyValue4.Hour);
            dateTime.Minute.ShouldBe(propertyValue4.Minute);
            dateTime.Second.ShouldBe(propertyValue4.Second);
            dateTime.Millisecond.ShouldBe(propertyValue4.Millisecond);
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_GenericJsonContentMessage_ReturnsValidInstance()
        {
            var target = GetTarget();

            var id = EnvelopeId.NewId();
            var from = Dummy.CreateNode();
            var pp = Dummy.CreateNode();
            var to = Dummy.CreateNode();

            string randomKey1 = "randomString1";
            string randomKey2 = "randomString2";
            string randomString1 = Dummy.CreateRandomStringExtended(50);
            string randomString2 = Dummy.CreateRandomStringExtended(50);

            var type = new MediaType(MediaType.DiscreteTypes.Application, MediaType.SubTypes.JSON, null);

            var propertyName1 = Dummy.CreateRandomStringExtended(10);
            var propertyName2 = Dummy.CreateRandomStringExtended(10);
            var propertyValue1 = Dummy.CreateRandomStringExtended(10);
            var propertyValue2 = (long)Dummy.CreateRandomInt(1000);


            string json =
                $"{{\"type\":\"{type}\",\"content\":{{\"{propertyName1.Escape()}\":\"{propertyValue1.Escape()}\",\"{propertyName2.Escape()}\":{propertyValue2}}},\"id\":\"{id}\",\"from\":\"{@from}\",\"pp\":\"{pp}\",\"to\":\"{to}\",\"metadata\":{{\"{randomKey1}\":\"{randomString1.Escape()}\",\"{randomKey2}\":\"{randomString2.Escape()}\"}}}}";

            var envelope = target.Deserialize(json);

            var message = envelope.ShouldBeOfType<Message>();
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

            var content = message.Content.ShouldBeOfType<JsonDocument>();

            Assert.IsTrue(content.ContainsKey(propertyName1));
            Assert.AreEqual(content[propertyName1], propertyValue1);
            Assert.IsTrue(content.ContainsKey(propertyName2));
            Assert.AreEqual(content[propertyName2], propertyValue2);
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_FireAndForgetTextMessage_ReturnsValidInstance()
        {
            // Arrange
            var target = GetTarget();
            var from = Dummy.CreateNode();
            var to = Dummy.CreateNode();
            var text = Dummy.CreateRandomStringExtended(50);
            string json =
                $"{{\"type\":\"text/plain\",\"content\":\"{text.Escape()}\",\"from\":\"{@from}\",\"to\":\"{to}\"}}";

            // Act
            var envelope = target.Deserialize(json);

            // Assert
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
        public void Deserialize_FireAndForgetChatStateMessage_ReturnsValidInstance()
        {
            // Arrange
            var target = GetTarget();
            var from = Dummy.CreateNode();
            var to = Dummy.CreateNode();
            var state = ChatStateEvent.Composing;
            string json =
                $"{{\"type\":\"application/vnd.lime.chatstate+json\",\"content\":{{\"state\":\"{state.ToString().ToCamelCase()}\"}},\"from\":\"{@from}\",\"to\":\"{to}\"}}";

            // Act
            var envelope = target.Deserialize(json);

            // Assert
            var message = envelope.ShouldBeOfType<Message>();
            Assert.AreEqual(from, message.From);
            Assert.AreEqual(to, message.To);

            Assert.IsNull(message.Id);
            Assert.IsNull(message.Pp);
            Assert.IsNull(message.Metadata);
            var textContent = message.Content.ShouldBeOfType<ChatState>();
            Assert.AreEqual(state, textContent.State);
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_ReceivedNotification_ReturnsValidInstance()
        {
            var target = GetTarget();

            var id = EnvelopeId.NewId();
            var from = Dummy.CreateNode();
            var pp = Dummy.CreateNode();
            var to = Dummy.CreateNode();

            string randomKey1 = "randomString1";
            string randomKey2 = "randomString2";
            string randomString1 = Dummy.CreateRandomStringExtended(50);
            string randomString2 = Dummy.CreateRandomStringExtended(50);

            var @event = Event.Received;

            string json =
                $"{{\"type\":\"application/vnd.lime.text+json\",\"event\":\"{@event.ToString().ToCamelCase()}\",\"id\":\"{id}\",\"from\":\"{@from}\",\"pp\":\"{pp}\",\"to\":\"{to}\",\"metadata\":{{\"{randomKey1}\":\"{randomString1.Escape()}\",\"{randomKey2}\":\"{randomString2.Escape()}\"}}}}";

            var envelope = target.Deserialize(json);
            var notification = envelope.ShouldBeOfType<Notification>();
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

        [Test]
        [Category("Deserialize")]
        public void Deserialize_FailedNotification_ReturnsValidInstance()
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

            var envelope = target.Deserialize(json);

            var notification = envelope.ShouldBeOfType<Notification>();
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

        [Test]
        [Category("Deserialize")]
        public void Deserialize_AuthenticatingSession_ReturnsValidInstance()
        {
            var target = GetTarget();

            var id = EnvelopeId.NewId();
            var from = Dummy.CreateNode();
            var pp = Dummy.CreateNode();
            var to = Dummy.CreateNode();

            var password = Dummy.CreateRandomString(10).ToBase64();

            string randomKey1 = "randomString1";
            string randomKey2 = "randomString2";
            string randomString1 = Dummy.CreateRandomStringExtended(50);
            string randomString2 = Dummy.CreateRandomStringExtended(50);

            var state = SessionState.Authenticating;

            string json =
                $"{{\"state\":\"{state.ToString().ToCamelCase()}\",\"scheme\":\"plain\",\"authentication\":{{\"password\":\"{password}\"}},\"id\":\"{id}\",\"from\":\"{@from}\",\"to\":\"{to}\",\"metadata\":{{\"{randomKey1}\":\"{randomString1.Escape()}\",\"{randomKey2}\":\"{randomString2.Escape()}\"}}}}";

            var envelope = target.Deserialize(json);

            var session = envelope.ShouldBeOfType<Session>();
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

        [Test]
        [Category("Deserialize")]
        public void Deserialize_FailedSessionNullProperties_ReturnsValidInstance()
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

            var envelope = target.Deserialize(json);

            var session = envelope.ShouldBeOfType<Session>();
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

        [Test]
        [Category("Deserialize")]
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

        [Test]
        [Category("Deserialize")]
        public void Deserialize_SessionAuthenticatingWithExternalAuthentication_ReturnsValidInstance()
        {
            // Arrange
            var serializer = GetTarget();
            var json = "{\"state\":\"authenticating\",\"scheme\":\"external\",\"authentication\":{\"token\":\"dFJZMTRXOE03NHBtcmZRNGY3NFo=\",\"issuer\":\"take.net\"},\"id\":\"ec9c196c-da09-43b0-923b-8ec162705c32\",\"from\":\"andre@takenet.com.br/MINELLI-NOTE\"}";

            // Act
            var envelope = serializer.Deserialize(json);

            // Assert
            var session = envelope.ShouldBeOfType<Session>();
            session.Scheme.ShouldBe(AuthenticationScheme.External);
            var plainAuthentication = session.Authentication.ShouldBeOfType<ExternalAuthentication>();
            plainAuthentication.Token.ShouldBe("dFJZMTRXOE03NHBtcmZRNGY3NFo=");
            plainAuthentication.Issuer.ShouldBe("take.net");
        }

        [Test]
        [Category("Deserialize")]
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

        [Test]
        [Category("Deserialize")]
        public void Deserialize_RandomResourceRequestCommand_ReturnsValidInstance()
        {
            // Arrange
            var target = GetTarget();
            var method = CommandMethod.Set;
            var id = EnvelopeId.NewId();
            DocumentTypeResolver.RegisterDocument<TestDocument>();

            string json =
                $"{{\"type\":\"application/vnd.takenet.testdocument+json\",\"resource\":{{\"double\":10.1, \"NullableDouble\": 10.2, \"Status\":\"Success\"}},\"method\":\"{method.ToString().ToCamelCase()}\",\"id\":\"{id}\"}}";

            // Act
            var envelope = target.Deserialize(json);

            // Assert
            var command = envelope.ShouldBeOfType<Command>();
            command.Type.ToString().ShouldBe(TestDocument.MIME_TYPE);
            command.Resource.ShouldNotBe(null);
            var document = command.Resource.ShouldBeOfType<TestDocument>();
            document.Double.ShouldBe(10.1d);
            document.NullableDouble.ShouldBe(10.2d);
            document.Status.ShouldBe(TestEnum.Success);
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_DocumentContainerDocumentCollectionMessage_ReturnsValidInstance()
        {
            // Arrange
            var id = EnvelopeId.NewId();
            var json =
                $"{{\"type\":\"application/vnd.lime.collection+json\",\"content\":{{\"total\":4,\"itemType\":\"application/vnd.lime.container+json\",\"items\":[{{\"type\":\"text/plain\",\"value\":\"text1\"}},{{\"type\":\"application/vnd.lime.account+json\",\"value\":{{\"fullName\":\"My Name\",\"photoUri\":\"http://url.com/resource\"}}}},{{\"type\":\"application/q9gn1nsz6y+json\",\"value\":{{\"o4s9txn80q\":\"}}2['\u00F23 /bdkc]\u00FA2,\u00BA &%f0j\u00F9u#\u00F2\u00FA9;\u00EC\\\"t}}#\u00F3(\u00E9a_94\u00E00q5m==\\\\\",\"ynpinmi0oq\":20,\"dkker2borf\":\"2016-04-13T16:24:49.729Z\",\"e98cyp215l\":{{\"ljwbthakfx\":\"\\\\@(m1g=q.-jql[)5#n,\u00E09\u00BA\u00A8kg~]t(x:<u\u00E1z'8?.-^_cvqkk\u00EC@n\",\"z4uih47pct\":19,\"nxp3n8km78\":\"2016-04-13T16:24:49.729Z\"}},\"sinvm70xls\":[{{\"ypdd57j78y\":\"<>5_\u00ECnb'!,b.ps8\u00EC=9\\\\o\\\\_*qc6#k0\u00E8]$j\u00E1=-u\u00E1\u00FAq\u00EC{{\u00E0r\u00F2\u00BAt\u00ED[\u00EC\",\"l3d24gigtt\":34,\"5ltasvmv3y\":\"2016-04-13T16:24:49.729Z\"}},{{\"1twigyljcf\":\"=!6-\u00F360 94fy2\u00A8e23q72\u00E0v\u00E9t(u!&[%\u00FA\u00E8#4f7\u00E0\u00ECkjv2n9=@pjp~\",\"ke4zjmvfbw\":46,\"2l7rf39qwq\":\"2016-04-13T16:24:49.729Z\"}},{{\"pckdtdowdc\":\"11\u00E9q>e:j,^;\u00F3\u00A8o@cs\u00F9@'r}}(3\u00EDe(=,uq*\u00F9(+!!..hd\u00E9;~.*(j=\u00A8\",\"5pfq4y1rmz\":24,\"foqvh78vau\":\"2016-04-13T16:24:49.729Z\"}}]}}}},{{\"type\":\"vxhfxfm3tz/hhnzgm4kmh\",\"value\":\"9nav5pkhswvsw7mh24r1b3agbgic43piylveh1z6xtfz77nibt\"}}]}},\"id\":\"{id}\",\"from\":\"9afudsyl@je29bkh1bs.com/yq1oh\",\"to\":\"9zpfpsuc@d63uusxbfq.com/btp7i\"}}";
            var target = GetTarget();

            // Act
            var envelope = target.Deserialize(json);

            // Assert
            envelope.ShouldNotBeNull();
            envelope.Id.ShouldBe(id);
            var message = envelope.ShouldBeOfType<Message>();
            var documentCollection = message.Content.ShouldBeOfType<DocumentCollection>();
            documentCollection.Total.ShouldBe(4);
            documentCollection.ItemType.ShouldBe(DocumentContainer.MediaType);
            documentCollection.Items.ShouldNotBeNull();
            documentCollection.Items.Length.ShouldBe(4);
            var container1 = documentCollection.Items[0].ShouldBeOfType<DocumentContainer>();
            var container2 = documentCollection.Items[1].ShouldBeOfType<DocumentContainer>();
            var container3 = documentCollection.Items[2].ShouldBeOfType<DocumentContainer>();
            var container4 = documentCollection.Items[3].ShouldBeOfType<DocumentContainer>();
            container1.Type.ShouldBe(PlainText.MediaType);
            var document1 = container1.Value.ShouldBeOfType<PlainText>();
            document1.Text.ShouldBe("text1");
            container2.Type.ShouldBe(Account.MediaType);
            var document2 = container2.Value.ShouldBeOfType<Account>();
            document2.FullName.ShouldBe("My Name");
            document2.PhotoUri.ShouldNotBeNull();
            document2.PhotoUri.ToString().ShouldBe("http://url.com/resource");
            container3.Type.ShouldBe(MediaType.Parse("application/q9gn1nsz6y+json"));
            var document3 = container3.Value.ShouldBeOfType<JsonDocument>();
            document3.Count.ShouldBeGreaterThan(0);
            container4.Type.ShouldBe(MediaType.Parse("vxhfxfm3tz/hhnzgm4kmh"));
            var document4 = container4.Value.ShouldBeOfType<PlainDocument>();
            document4.Value.ShouldBe("9nav5pkhswvsw7mh24r1b3agbgic43piylveh1z6xtfz77nibt");
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_DocumentSelectMessage_ReturnsValidInstance()
        {
            // Arrange            
            var json = "{\"id\":\"message-id\",\"from\":\"andreb@msging.net\",\"type\":\"application/vnd.lime.document-select+json\",\"content\":{\"header\":{\"type\":\"application/vnd.lime.media-link+json\",\"value\":{\"title\":\"Welcome to Peter\'s Hats\",\"text\":\"We\'ve got the right hat for everyone.\",\"type\":\"image/jpeg\",\"uri\":\"http://petersapparel.parseapp.com/img/item100-thumb.png\"}},\"options\":[{\"label\":{\"type\":\"application/vnd.lime.web-link+json\",\"value\":{\"text\":\"View Website\",\"uri\":\"https://petersapparel.parseapp.com/view_item?item_id=100\"}}},{\"label\":{\"type\":\"text/plain\",\"value\":\"Start Chatting\"},\"value\":{\"type\":\"application/json\",\"value\":{\"key\":\"key1\",\"value\":1}}}]}}";
            var target = GetTarget();

            // Act
            var envelope = target.Deserialize(json);

            // Assert
            envelope.ShouldNotBeNull();
            envelope.Id.ShouldBe("message-id");
            envelope.From.ShouldNotBeNull();
            envelope.From.ToString().ShouldBe("andreb@msging.net");
            envelope.To.ShouldBeNull();
            var message = envelope.ShouldBeOfType<Message>();
            var documentSelect = message.Content.ShouldBeOfType<DocumentSelect>();
            documentSelect.Header.ShouldNotBeNull();
            var header = documentSelect.Header.Value.ShouldBeOfType<MediaLink>();
            header.Title.ShouldBe("Welcome to Peter's Hats");
            header.Text.ShouldBe("We've got the right hat for everyone.");
            header.Type.ToString().ShouldBe("image/jpeg");
            header.Uri.ToString().ShouldBe("http://petersapparel.parseapp.com/img/item100-thumb.png");
            documentSelect.Options.ShouldNotBeNull();
            documentSelect.Options.Length.ShouldBe(2);
            var option1Label = documentSelect.Options[0].Label.Value.ShouldBeOfType<WebLink>();
            option1Label.Text.ShouldBe("View Website");
            option1Label.Uri.ToString().ShouldBe("https://petersapparel.parseapp.com/view_item?item_id=100");
            documentSelect.Options[0].Value.ShouldBeNull();
            var option2Label = documentSelect.Options[1].Label.Value.ShouldBeOfType<PlainText>();
            option2Label.Text.ShouldBe("Start Chatting");
            var option2Value = documentSelect.Options[1].Value.Value.ShouldBeOfType<JsonDocument>();
            option2Value.ShouldContainKeyAndValue("key", "key1");
            option2Value.ShouldContainKeyAndValue("value", (long)1);
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_CommandWithMessage_ReturnsValidInstance()
        {
            // Arrange
            var json =
                "{\"uri\":\"/sessions/92d8f9fb-0857-4b1a-8f50-f0f37dedc140?expiration=635961514943253147\",\"type\":\"application/vnd.lime.message+json\",\"resource\":{\"id\":\"52e7804c-e483-4f65-85e2-52abc007b35b\",\"from\":\"andreb@msging.net/default\",\"to\":\"joao@msging.net\",\"type\":\"text/plain\",\"content\":\"Banana\",\"metadata\":{\"$internalId\":\"66ec04ec-a278-4251-8ff3-6931bf73e04f\"}},\"method\":\"set\",\"id\":\"66ec04ec-a278-4251-8ff3-6931bf73e04f\"}";

            var target = GetTarget();

            // Act
            var actual = target.Deserialize(json);

            // Assert
            var actualCommand = actual.ShouldBeOfType<Command>();
            actualCommand.Resource.ShouldNotBeNull();
            var jsonDocument = actualCommand.Resource.ShouldBeOfType<JsonDocument>();
            var jObject = JObject.FromObject(jsonDocument, target.Serializer);
            var message = jObject.ToObject(typeof(Message), target.Serializer).ShouldBeOfType<Message>();
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_ObserveCommandWithoutId_ReturnsValidInstance()
        {
            // Arrange
            var json =
                "{\"uri\":\"/sessions/92d8f9fb-0857-4b1a-8f50-f0f37dedc140?expiration=635961514943253147\",\"type\":\"application/vnd.lime.message+json\",\"resource\":{\"id\":\"52e7804c-e483-4f65-85e2-52abc007b35b\",\"from\":\"andreb@msging.net/default\",\"to\":\"joao@msging.net\",\"type\":\"text/plain\",\"content\":\"Banana\",\"metadata\":{\"$internalId\":\"66ec04ec-a278-4251-8ff3-6931bf73e04f\"}},\"method\":\"observe\"}";

            var target = GetTarget();

            // Act
            var actual = target.Deserialize(json);

            // Assert
            var actualCommand = actual.ShouldBeOfType<Command>();
            actualCommand.Resource.ShouldNotBeNull();
            actualCommand.Id.ShouldBeNull();
            actualCommand.Method.ShouldBe(CommandMethod.Observe);
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_WeblinkMessage_ReturnsValidInstance()
        {
            // Arrange
            var json =
                "{\"type\":\"application/vnd.lime.web-link+json\",\"content\":{\"uri\":\"http://e0x0rkuaof.com:9288/\",\"previewUri\":\"http://pcmcjxomhd.com:9875/\",\"previewType\":\"image/jpeg\",\"text\":\"b9s38pra6s7w7b4w1jca6lzf9zp8927ciy4lwdsa3y1gc2ekiw\"},\"id\":\"25058656-ea3e-4f2a-9b27-fe14d1470796\",\"from\":\"6fjghzjm@3j9saev4nj.com/gtax0\",\"to\":\"cghusdgu@f0m512bqfb.com/jjjak\"}";
            var target = GetTarget();

            // Act
            var actual = target.Deserialize(json);

            // Assert
            var actualMessage = actual.ShouldBeOfType<Message>();
            var webLink = actualMessage.Content.ShouldBeOfType<WebLink>();
            webLink.Uri.ShouldNotBeNull();
            webLink.Uri.OriginalString.ShouldBe("http://e0x0rkuaof.com:9288/");
            webLink.PreviewUri.ShouldNotBeNull();
            webLink.PreviewUri.OriginalString.ShouldBe("http://pcmcjxomhd.com:9875/");
            webLink.PreviewType.ShouldNotBeNull();
            webLink.PreviewType.ToString().ShouldBe("image/jpeg");
            webLink.Text.ShouldBe("b9s38pra6s7w7b4w1jca6lzf9zp8927ciy4lwdsa3y1gc2ekiw");

        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_InvalidWeblinkMessage_ReturnsJsonDocument()
        {
            // Arrange
            var json =
                "{\"type\":\"application/vnd.lime.web-link+json\",\"content\":{\"uri\":{\"value\":\"http://e0x0rkuaof.com:9288/\"},\"previewUri\":\"http://pcmcjxomhd.com:9875/\",\"previewType\":\"image/jpeg\",\"text\":\"b9s38pra6s7w7b4w1jca6lzf9zp8927ciy4lwdsa3y1gc2ekiw\"},\"id\":\"25058656-ea3e-4f2a-9b27-fe14d1470796\",\"from\":\"6fjghzjm@3j9saev4nj.com/gtax0\",\"to\":\"cghusdgu@f0m512bqfb.com/jjjak\"}";
            var target = GetTarget();

            // Act
            var actual = target.Deserialize(json);

            // Assert
            var actualMessage = actual.ShouldBeOfType<Message>();
            actualMessage.Type.ShouldBe(WebLink.MediaType);
            var jsonDocument = actualMessage.Content.ShouldBeOfType<JsonDocument>();
            jsonDocument["uri"].ShouldNotBeNull();
            var uri = jsonDocument["uri"].ShouldBeOfType<Dictionary<string, object>>();
            uri["value"].ShouldBe("http://e0x0rkuaof.com:9288/");
            jsonDocument["previewUri"].ShouldNotBeNull();
            jsonDocument["previewUri"].ShouldBe("http://pcmcjxomhd.com:9875/");
            jsonDocument["previewType"].ShouldNotBeNull();
            jsonDocument["previewType"].ToString().ShouldBe("image/jpeg");
            jsonDocument["text"].ShouldBe("b9s38pra6s7w7b4w1jca6lzf9zp8927ciy4lwdsa3y1gc2ekiw");
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_InvalidMedialinkMessage_ReturnsJsonDocument()
        {
            // Arrange
            var json =
                $"{{ \"id\": \"messageId\", \"type\": \"application/vnd.lime.media-link+json\", \"content\": {{ \"type\": \"image/\", \"uri\": \"http://e0x0rkuaof.com:9288/\" }} }}";

            var target = GetTarget();

            // Act
            var actual = target.Deserialize(json);

            // Assert
            var actualMessage = actual.ShouldBeOfType<Message>();
            actualMessage.Id.ShouldBe("messageId");
            actualMessage.Type.ShouldBe(MediaLink.MediaType);
            var jsonDocument = actualMessage.Content.ShouldBeOfType<JsonDocument>();
            jsonDocument["uri"].ShouldNotBeNull();
            jsonDocument["uri"].ShouldBe("http://e0x0rkuaof.com:9288/");
            jsonDocument["type"].ShouldNotBeNull();
            jsonDocument["type"].ToString().ShouldBe("image/");
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_LocationMessage_ReturnsJsonDocument()
        {
            // Arrange
            var json =
                "{\"type\":\"application/vnd.lime.location+json\",\"content\":{\"latitude\":\"*\",\"longitude\":\"*\",\"altitude\":853,\"text\":\"*\"},\"id\":\"321fd00b-92c5-492f-8e68-dadb0d1905b0\",\"from\":\"leaseplanhmg@msging.net/az-iris2\",\"to\":\"8425b013-793c-468a-a77f-8ce5ce553868.leaseplanhmg@0mn.io/default\",\"metadata\":{\"#stateName\":\"Y.1.1 - Detalhes do agendamento\",\"#stateId\":\"120f4b27-0bd5-41c6-a103-fa6d7b1066ad\",\"#messageId\":\"c520e0f1-990e-432d-b803-7d342779fc6b\",\"$originator\":\"leaseplanhmg@msging.net\",\"$claims\":\"Node=leaseplanhmg@msging.net/az-iris2;Identity=leaseplanhmg@msging.net;DomainRole=Member;AuthenticationScheme=Transport\"}}";

            var target = GetTarget();

            // Act
            var actual = target.Deserialize(json);

            // Assert
            var actualMessage = actual.ShouldBeOfType<Message>();
            var jsonDocument = actualMessage.Content.ShouldBeOfType<JsonDocument>();
            jsonDocument["latitude"].ShouldBe("*");
            jsonDocument["longitude"].ShouldBe("*");
            jsonDocument["altitude"].ShouldBe(853);
            jsonDocument["text"].ShouldBe("*");
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_WeblinkWithEscapedUriMessage_ReturnsValidInstance()
        {
            // Arrange
            var json =
                "{\"type\":\"application/vnd.lime.web-link+json\",\"content\":{\"uri\":\"http://fake.domain.com:5678?email=anyone%40gmail.com&address=https%3A%2F%2Fgoogle.com%3Fq%3Dbanana%2520azul\"},\"id\":\"25058656-ea3e-4f2a-9b27-fe14d1470796\",\"from\":\"6fjghzjm@3j9saev4nj.com/gtax0\",\"to\":\"cghusdgu@f0m512bqfb.com/jjjak\"}";
            var target = GetTarget();

            // Act
            var actual = target.Deserialize(json);

            // Assert
            var actualMessage = actual.ShouldBeOfType<Message>();
            var webLink = actualMessage.Content.ShouldBeOfType<WebLink>();
            webLink.Uri.ShouldNotBeNull();
            webLink.Uri.OriginalString.ShouldBe("http://fake.domain.com:5678?email=anyone%40gmail.com&address=https%3A%2F%2Fgoogle.com%3Fq%3Dbanana%2520azul");
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_SelectMessage_ReturnsValidInstance()
        {
            // Arrange
            var json =
                "{\"type\":\"application/vnd.lime.select+json\",\"content\":{\"text\":\"Select an option:\",\"options\":[{\"text\":\"Option 1\"}]},\"id\":\"25058656-ea3e-4f2a-9b27-fe14d1470796\",\"from\":\"6fjghzjm@3j9saev4nj.com/gtax0\",\"to\":\"cghusdgu@f0m512bqfb.com/jjjak\"}";
            var target = GetTarget();

            // Act
            var actual = target.Deserialize(json);

            // Assert
            var actualMessage = actual.ShouldBeOfType<Message>();
            var select = actualMessage.Content.ShouldBeOfType<Select>();
            select.Text.ShouldBe("Select an option:");
            select.Options.ShouldNotBeNull();
            select.Options.Length.ShouldBe(1);
            select.Options[0].Text.ShouldBe("Option 1");
        }


        [Test]
        [Category("Deserialize")]
        public void Deserialize_SelectWithOptionWithNullType_ReturnsValidInstance()
        {
            // Arrange
            var json =
                "{\"type\":\"application/vnd.lime.select+json\",\"content\":{\"text\":\"Select an option:\",\"options\":[{\"text\":\"Option 1\",\"value\":null,\"type\":null}]},\"id\":\"25058656-ea3e-4f2a-9b27-fe14d1470796\",\"from\":\"6fjghzjm@3j9saev4nj.com/gtax0\",\"to\":\"cghusdgu@f0m512bqfb.com/jjjak\"}";
            var target = GetTarget();

            // Act
            var actual = target.Deserialize(json);

            // Assert
            var actualMessage = actual.ShouldBeOfType<Message>();
            var select = actualMessage.Content.ShouldBeOfType<Select>();
            select.Text.ShouldBe("Select an option:");
            select.Options.ShouldNotBeNull();
            select.Options.Length.ShouldBe(1);
            select.Options[0].Text.ShouldBe("Option 1");
            select.Options[0].Type.ShouldBeNull();
            select.Options[0].Value.ShouldBeNull();
        }

        [Test]
        [Category("Deserialize")]
        public void Deserialize_RawJsonMessage_ReturnsJsonDocument()
        {
            // Arrange
            var json =
                "{\"id\":\"1\",\"to\":\"destination@limeprotocol.org\",\"type\":\"application\\/json\",\"content\":{\"expectUserResponse\":true,\"expectedInputs\":[{\"inputPrompt\":{\"richInitialPrompt\":{\"items\":[{\"simpleResponse\":{\"textToSpeech\":\"Which of these looks good?\"}}]}},\"possibleIntents\":[{\"intent\":\"actions.intent.OPTION\",\"inputValueData\":{\"@type\":\"type.googleapis.com\\/google.actions.v2.OptionValueSpec\",\"listSelect\":{\"items\":[{\"optionInfo\":{\"key\":\"first\",\"synonyms\":[\"synonym of KEY_ONE 1\",\"synonym of KEY_ONE 2\"]},\"description\":\"y\",\"title\":\"Number one\"},{\"optionInfo\":{\"key\":\"second\",\"synonyms\":[\"synonym of KEY_TWO 1\",\"synonym of KEY_TWO 2\"]},\"description\":\"x\",\"title\":\"Number two\"},{\"optionInfo\":{\"key\":\"third\",\"synonyms\":[\"synonym of KEY_THREE 1\",\"synonym of KEY_THREE 3\"]},\"description\":\"x\",\"title\":\"Number three\"}]}}}]}],\"conversationToken\":\"{\\\"data\\\":{}}\",\"userStorage\":\"{\\\"data\\\":{}}\"}}";
            var target = GetTarget();

            // Act
            var actual = target.Deserialize(json);

            // Assert
            var actualMessage = actual.ShouldBeOfType<Message>();
            var jsonDocument = actualMessage.Content.ShouldBeOfType<JsonDocument>();
            var expectUserResponse = jsonDocument["expectUserResponse"].ShouldBeOfType<bool>();
            expectUserResponse.ShouldBeTrue();
            var array = jsonDocument["expectedInputs"].ShouldBeAssignableTo<JArray>();
            array.Count.ShouldBe(1);
            var jObject = array[0];
            jObject["inputPrompt"].ShouldBeOfType<JObject>();
        }

        [Test]
        [Category("Serialize")]
        [Category("Deserialize")]
        public void DeserializeAndSerialize_JsonDocument_ShouldReturnValidDocument()
        {
            // Arrange
            var json =
                "{\"id\":\"1\",\"to\":\"destination@limeprotocol.org\",\"type\":\"application\\/json\",\"content\":{\"expectUserResponse\":true,\"expectedInputs\":[{\"inputPrompt\":{\"richInitialPrompt\":{\"items\":[{\"simpleResponse\":{\"textToSpeech\":\"Which of these looks good?\"}}]}},\"possibleIntents\":[{\"intent\":\"actions.intent.OPTION\",\"inputValueData\":{\"@type\":\"type.googleapis.com\\/google.actions.v2.OptionValueSpec\",\"listSelect\":{\"items\":[{\"optionInfo\":{\"key\":\"first\",\"synonyms\":[\"synonym of KEY_ONE 1\",\"synonym of KEY_ONE 2\"]},\"description\":\"y\",\"title\":\"Number one\"},{\"optionInfo\":{\"key\":\"second\",\"synonyms\":[\"synonym of KEY_TWO 1\",\"synonym of KEY_TWO 2\"]},\"description\":\"x\",\"title\":\"Number two\"},{\"optionInfo\":{\"key\":\"third\",\"synonyms\":[\"synonym of KEY_THREE 1\",\"synonym of KEY_THREE 3\"]},\"description\":\"x\",\"title\":\"Number three\"}]}}}]}],\"conversationToken\":\"{\\\"data\\\":{}}\",\"userStorage\":\"{\\\"data\\\":{}}\"}}";
            var target = GetTarget();

            // Act
            var actualDocument = target.Deserialize(target.Serialize(target.Deserialize(json)));

            // Assert
            var actualMessage = actualDocument.ShouldBeOfType<Message>();
            var jsonDocument = actualMessage.Content.ShouldBeOfType<JsonDocument>();
            var expectUserResponse = jsonDocument["expectUserResponse"].ShouldBeOfType<bool>();
            expectUserResponse.ShouldBeTrue();
            var array = jsonDocument["expectedInputs"].ShouldBeAssignableTo<JArray>();
            array.Count.ShouldBe(1);
            var jObject = array[0];
            jObject["inputPrompt"].ShouldBeOfType<JObject>();
        }
        #endregion

        #region Converter
        [Test]
        [Category("Converter")]
        public void TryAddConverter_ShouldIgnoreDuplicate()
        {
            // Arrange
            var target = GetTarget();

            // Act
            var firstInsertResult = target.TryAddConverter(new DummyJsonConverter1());
            var secondInsertResult = target.TryAddConverter(new DummyJsonConverter1());

            // Assert
            firstInsertResult.ShouldBeTrue();
            secondInsertResult.ShouldBeTrue();
            target
                .Serializer
                .Converters
                .Where(converter => converter is DummyJsonConverter1)
                .Count()
                .ShouldBe(2);
        }

        [Test]
        [Category("Converter")]
        public void TryAddConverter_ShouldNotIgnoreDuplicate()
        {
            // Arrange
            var target = GetTarget();

            // Act
            var firstInsertResult = target.TryAddConverter(new DummyJsonConverter1(), false);
            var secondInsertResult = target.TryAddConverter(new DummyJsonConverter1(), false);

            // Assert
            firstInsertResult.ShouldBeTrue();
            secondInsertResult.ShouldBeFalse();
            target
                .Serializer
                .Converters
                .Where(converter => converter is DummyJsonConverter1)
                .Count()
                .ShouldBe(1);
        }


        [Test]
        [Category("Converter")]
        public void TryAddConverter_ShouldHaveDocumentJsonConverterAsTheLast()
        {
            // Arrange
            var target = GetTarget();

            // Act
            var firstInsertResult = target.TryAddConverter(new DummyJsonConverter1());
            var secondInsertResult = target.TryAddConverter(new DummyJsonConverter2());

            // Assert
            firstInsertResult.ShouldBe(true);
            secondInsertResult.ShouldBe(true);
            target
                .Serializer
                .Converters
                .Last()
                .ShouldBeOfType(typeof(DocumentJsonConverter));
        }
        #endregion
    }

    internal abstract class DummyObject : Document { public DummyObject(int n) : base(MediaType.Parse($"application/dummy_object{n}+json")) { } public string Property { get; set; } }
    internal sealed class DummyObject1 : DummyObject { public DummyObject1() : base(1) { } }
    internal sealed class DummyObject2 : DummyObject { public DummyObject2() : base(2) { } }

    internal sealed class DummyJsonConverter1 : JsonConverter
    {
        public static string PropertyDefaultValueConverted => "Dummy Property value";
        public override bool CanRead => true;
        public override bool CanWrite => false;
        public override bool CanConvert(Type objectType) => objectType == typeof(DummyObject1);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return new DummyObject1() { Property = PropertyDefaultValueConverted };
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    internal sealed class DummyJsonConverter2 : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(DummyObject2);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}