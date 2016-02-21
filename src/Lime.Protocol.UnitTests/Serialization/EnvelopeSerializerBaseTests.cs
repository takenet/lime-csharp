using Lime.Messaging.Contents;
using Lime.Messaging.Resources;
using Lime.Protocol.Security;
using Lime.Protocol.Serialization;
using Xunit;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Lime.Protocol.UnitTests.Serialization
{
	
	public abstract class EnvelopeSerializerBaseTests
	{
		protected abstract IEnvelopeSerializer GetTarget();

		#region Serialize

		[Fact]
		[Trait("Category", "Serialize")]
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

			Assert.True(resultString.HasValidJsonStackedBrackets());
			Assert.True(resultString.ContainsJsonProperty(Envelope.ID_KEY, command.Id));
			Assert.True(resultString.ContainsJsonProperty(Envelope.FROM_KEY, command.From));
			Assert.True(resultString.ContainsJsonProperty(Envelope.PP_KEY, command.Pp));
			Assert.True(resultString.ContainsJsonProperty(Envelope.TO_KEY, command.To));
			Assert.True(resultString.ContainsJsonProperty(Command.METHOD_KEY, command.Method));
			Assert.True(resultString.ContainsJsonProperty(Command.URI_KEY, command.Uri));
			
			
			Assert.True(resultString.ContainsJsonProperty(Command.METHOD_KEY, command.Method));
			Assert.True(resultString.ContainsJsonProperty(metadataKey1, metadataValue1));
			Assert.True(resultString.ContainsJsonProperty(metadataKey2, metadataValue2));
			
			Assert.False(resultString.ContainsJsonKey(Command.STATUS_KEY));
			Assert.False(resultString.ContainsJsonKey(Command.REASON_KEY));
			Assert.False(resultString.ContainsJsonKey(Command.TYPE_KEY));
			Assert.False(resultString.ContainsJsonKey(Command.RESOURCE_KEY));
		}

		[Fact]
		[Trait("Category", "Serialize")]
		public void Serialize_RelativeUriRequestCommand_ReturnsValidJsonString()
		{
			var target = GetTarget();

			var resource = Dummy.CreateJsonDocument();

			var command = Dummy.CreateCommand(resource);
			command.Pp = Dummy.CreateNode();
			command.Method = CommandMethod.Set;
			command.Uri = Dummy.CreateRelativeLimeUri();

			var resultString = target.Serialize(command);

			Assert.True(resultString.HasValidJsonStackedBrackets());
			Assert.True(resultString.ContainsJsonProperty(Envelope.ID_KEY, command.Id));
			Assert.True(resultString.ContainsJsonProperty(Envelope.FROM_KEY, command.From));
			Assert.True(resultString.ContainsJsonProperty(Envelope.PP_KEY, command.Pp));
			Assert.True(resultString.ContainsJsonProperty(Envelope.TO_KEY, command.To));
			Assert.True(resultString.ContainsJsonProperty(Command.METHOD_KEY, command.Method));
			Assert.True(resultString.ContainsJsonProperty(Command.URI_KEY, command.Uri));
			Assert.True(resultString.ContainsJsonProperty(Command.METHOD_KEY, command.Method));
			Assert.True(resultString.ContainsJsonProperty(Command.TYPE_KEY, resource.GetMediaType()));
			Assert.True(resultString.ContainsJsonKey(Command.RESOURCE_KEY));

			Assert.False(resultString.ContainsJsonKey(Command.METADATA_KEY));
			Assert.False(resultString.ContainsJsonKey(Command.STATUS_KEY));
			Assert.False(resultString.ContainsJsonKey(Command.REASON_KEY));            
		}

		[Fact]
		[Trait("Category", "Serialize")]
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

			Assert.True(resultString.HasValidJsonStackedBrackets());
			Assert.True(resultString.ContainsJsonProperty(Envelope.ID_KEY, command.Id));
			Assert.True(resultString.ContainsJsonProperty(Envelope.FROM_KEY, command.From));
			Assert.True(resultString.ContainsJsonProperty(Envelope.PP_KEY, command.Pp));
			Assert.True(resultString.ContainsJsonProperty(Envelope.TO_KEY, command.To));
			Assert.True(resultString.ContainsJsonProperty(Command.METHOD_KEY, command.Method));
			Assert.True(resultString.ContainsJsonProperty(Command.TYPE_KEY, command.Resource.GetMediaType()));
			Assert.True(resultString.ContainsJsonKey(Command.RESOURCE_KEY));
			Assert.True(resultString.ContainsJsonProperty(Command.METHOD_KEY, command.Method));
			Assert.True(resultString.ContainsJsonProperty(metadataKey1, metadataValue1));
			Assert.True(resultString.ContainsJsonProperty(metadataKey2, metadataValue2));

			Assert.True(resultString.ContainsJsonProperty(Capability.CONTENT_TYPES_KEY, resource.ContentTypes));
			Assert.True(resultString.ContainsJsonProperty(Capability.RESOURCE_TYPES_KEY, resource.ResourceTypes));

			Assert.False(resultString.ContainsJsonKey(Command.STATUS_KEY));
			Assert.False(resultString.ContainsJsonKey(Command.REASON_KEY));
		}

		[Fact]
		[Trait("Category", "Serialize")]
		public void Serialize_AccountRequestCommand_ReturnsValidJsonString()
		{
			var target = GetTarget();

			var resource = Dummy.CreateAccount();
			var command = Dummy.CreateCommand(resource);
			command.Method = CommandMethod.Get;

			var resultString = target.Serialize(command);

			Assert.True(resultString.HasValidJsonStackedBrackets());
			Assert.True(resultString.ContainsJsonProperty(Envelope.ID_KEY, command.Id));
			Assert.True(resultString.ContainsJsonProperty(Envelope.FROM_KEY, command.From));
			Assert.True(resultString.ContainsJsonProperty(Envelope.TO_KEY, command.To));
			Assert.True(resultString.ContainsJsonProperty(Command.METHOD_KEY, command.Method));
			Assert.True(resultString.ContainsJsonProperty(Command.TYPE_KEY, command.Resource.GetMediaType()));
			Assert.True(resultString.ContainsJsonKey(Command.RESOURCE_KEY));
			Assert.True(resultString.ContainsJsonProperty(Command.METHOD_KEY, command.Method));

			Assert.True(resultString.ContainsJsonProperty(Account.FULL_NAME_KEY, resource.FullName));
			Assert.True(resultString.ContainsJsonProperty(Account.PHOTO_URI_KEY, resource.PhotoUri));

			Assert.False(resultString.ContainsJsonKey(Envelope.PP_KEY));
			Assert.False(resultString.ContainsJsonKey(Command.METADATA_KEY));
			Assert.False(resultString.ContainsJsonKey(Command.STATUS_KEY));
			Assert.False(resultString.ContainsJsonKey(Command.REASON_KEY));
		}

		[Fact]
		[Trait("Category", "Serialize")]
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

			Assert.True(resultString.HasValidJsonStackedBrackets());
			Assert.True(resultString.ContainsJsonProperty(Envelope.ID_KEY, command.Id));
			Assert.True(resultString.ContainsJsonProperty(Envelope.FROM_KEY, command.From));
			Assert.True(resultString.ContainsJsonProperty(Envelope.PP_KEY, command.Pp));
			Assert.True(resultString.ContainsJsonProperty(Envelope.TO_KEY, command.To));
			Assert.True(resultString.ContainsJsonProperty(Command.METHOD_KEY, command.Method));
			Assert.True(resultString.ContainsJsonProperty(Command.TYPE_KEY, command.Resource.GetMediaType()));
			Assert.True(resultString.ContainsJsonKey(Command.RESOURCE_KEY));
			Assert.True(resultString.ContainsJsonProperty(Command.METHOD_KEY, command.Method));
			Assert.True(resultString.ContainsJsonProperty(metadataKey1, metadataValue1));
			Assert.True(resultString.ContainsJsonProperty(metadataKey2, metadataValue2));

			Assert.True(resultString.ContainsJsonKey(DocumentCollection.ITEMS_KEY));
			Assert.True(resultString.ContainsJsonKey(DocumentCollection.TOTAL_KEY));
			var contacts = resource.Items.Cast<Contact>().ToArray();
			Assert.True(resultString.ContainsJsonProperty(Contact.IDENTITY_KEY, contacts[0].Identity));
			Assert.True(resultString.ContainsJsonProperty(Contact.NAME_KEY, contacts[0].Name));
			Assert.True(resultString.ContainsJsonProperty(Contact.IS_PENDING_KEY, contacts[0].IsPending));
			Assert.True(resultString.ContainsJsonProperty(Contact.SHARE_ACCOUNT_INFO_KEY, contacts[0].ShareAccountInfo));
			Assert.True(resultString.ContainsJsonProperty(Contact.IDENTITY_KEY, contacts[1].Identity));
			Assert.True(resultString.ContainsJsonProperty(Contact.NAME_KEY, contacts[1].Name));
			Assert.True(resultString.ContainsJsonProperty(Contact.SHARE_PRESENCE_KEY, contacts[1].SharePresence));
			Assert.True(resultString.ContainsJsonProperty(Contact.IDENTITY_KEY, contacts[2].Identity));
			Assert.True(resultString.ContainsJsonProperty(Contact.NAME_KEY, contacts[2].Name));

			Assert.True(resultString.ContainsJsonKey(Command.STATUS_KEY));
			Assert.False(resultString.ContainsJsonKey(Command.REASON_KEY));
		}

		[Fact]
		[Trait("Category", "Serialize")]
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

			Assert.True(resultString.HasValidJsonStackedBrackets());
			Assert.True(resultString.ContainsJsonProperty(Envelope.ID_KEY, command.Id));
			Assert.True(resultString.ContainsJsonProperty(Envelope.FROM_KEY, command.From));
			Assert.True(resultString.ContainsJsonProperty(Envelope.PP_KEY, command.Pp));
			Assert.True(resultString.ContainsJsonProperty(Envelope.TO_KEY, command.To));
			Assert.True(resultString.ContainsJsonProperty(Command.METHOD_KEY, command.Method));
			Assert.True(resultString.ContainsJsonProperty(Command.TYPE_KEY, command.Resource.GetMediaType()));
			Assert.True(resultString.ContainsJsonKey(Command.RESOURCE_KEY));
			Assert.True(resultString.ContainsJsonProperty(Command.METHOD_KEY, command.Method));
			Assert.True(resultString.ContainsJsonProperty(metadataKey1, metadataValue1));
			Assert.True(resultString.ContainsJsonProperty(metadataKey2, metadataValue2));

			Assert.True(resultString.ContainsJsonKey(DocumentCollection.ITEMS_KEY));
			Assert.True(resultString.ContainsJsonProperty(DocumentCollection.ITEM_TYPE_KEY, contact1.GetMediaType()));
			Assert.True(resultString.ContainsJsonProperty(DocumentCollection.TOTAL_KEY, resource.Items.Length));
  
			Assert.True(resultString.ContainsJsonKey(Command.STATUS_KEY));
			Assert.False(resultString.ContainsJsonKey(Command.REASON_KEY));
		}

		[Fact]
		[Trait("Category", "Serialize")]
		public void Serialize_PresenceRequestCommand_ReturnsValidJsonString()
		{
			var target = GetTarget();

			var resource = Dummy.CreatePresence();
			var command = Dummy.CreateCommand(resource);
			command.Method = CommandMethod.Get;

			var resultString = target.Serialize(command);

			Assert.True(resultString.HasValidJsonStackedBrackets());
			Assert.True(resultString.ContainsJsonProperty(Envelope.ID_KEY, command.Id));
			Assert.True(resultString.ContainsJsonProperty(Envelope.FROM_KEY, command.From));
			Assert.True(resultString.ContainsJsonProperty(Envelope.TO_KEY, command.To));
			Assert.True(resultString.ContainsJsonProperty(Command.METHOD_KEY, command.Method));
			Assert.True(resultString.ContainsJsonProperty(Command.TYPE_KEY, command.Resource.GetMediaType()));
			Assert.True(resultString.ContainsJsonKey(Command.RESOURCE_KEY));
			Assert.True(resultString.ContainsJsonProperty(Command.METHOD_KEY, command.Method));

			Assert.True(resultString.ContainsJsonProperty(Presence.MESSAGE_KEY, resource.Message));
			Assert.True(resultString.ContainsJsonProperty(Presence.LAST_SEEN_KEY, resource.LastSeen));


			Assert.False(resultString.ContainsJsonKey(Envelope.PP_KEY));
			Assert.False(resultString.ContainsJsonKey(Command.METADATA_KEY));
			Assert.False(resultString.ContainsJsonProperty(Command.STATUS_KEY, "pending"));
			Assert.False(resultString.ContainsJsonKey(Command.REASON_KEY));
		}

		[Fact]
		[Trait("Category", "Serialize")]
		public void Serialize_PresenceRequestWithOffsetDateCommand_ReturnsValidJsonString()
		{
			var target = GetTarget();

			var resource = Dummy.CreatePresence();
			resource.LastSeen = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(-5));
			var command = Dummy.CreateCommand(resource);
			command.Method = CommandMethod.Get;

			var resultString = target.Serialize(command);

			Assert.True(resultString.HasValidJsonStackedBrackets());
			Assert.True(resultString.ContainsJsonProperty(Envelope.ID_KEY, command.Id));
			Assert.True(resultString.ContainsJsonProperty(Envelope.FROM_KEY, command.From));
			Assert.True(resultString.ContainsJsonProperty(Envelope.TO_KEY, command.To));
			Assert.True(resultString.ContainsJsonProperty(Command.METHOD_KEY, command.Method));
			Assert.True(resultString.ContainsJsonProperty(Command.TYPE_KEY, command.Resource.GetMediaType()));
			Assert.True(resultString.ContainsJsonKey(Command.RESOURCE_KEY));
			Assert.True(resultString.ContainsJsonProperty(Command.METHOD_KEY, command.Method));

			Assert.True(resultString.ContainsJsonProperty(Presence.MESSAGE_KEY, resource.Message));
			Assert.True(resultString.ContainsJsonProperty(Presence.LAST_SEEN_KEY, resource.LastSeen));

			Assert.False(resultString.ContainsJsonKey(Envelope.PP_KEY));
			Assert.False(resultString.ContainsJsonKey(Command.METADATA_KEY));
			Assert.False(resultString.ContainsJsonProperty(Command.STATUS_KEY, "pending"));
			Assert.False(resultString.ContainsJsonKey(Command.REASON_KEY));
		}

		[Fact]
		[Trait("Category", "Serialize")]
		public void Serialize_FailurePingResponseCommand_ReturnsValidJsonString()
		{
			var target = GetTarget();

			var command = Dummy.CreateCommand(status: CommandStatus.Failure);
			command.Reason = Dummy.CreateReason();

			var resultString = target.Serialize(command);

			Assert.True(resultString.HasValidJsonStackedBrackets());
			Assert.True(resultString.ContainsJsonProperty(Envelope.FROM_KEY, command.From));
			Assert.True(resultString.ContainsJsonProperty(Envelope.ID_KEY, command.Id));
			Assert.True(resultString.ContainsJsonProperty(Envelope.TO_KEY, command.To));
			Assert.True(resultString.ContainsJsonProperty(Command.METHOD_KEY, command.Method));
			Assert.True(resultString.ContainsJsonProperty(Command.STATUS_KEY, command.Status));
			Assert.True(resultString.ContainsJsonKey(Command.REASON_KEY));
			Assert.True(resultString.ContainsJsonProperty(Reason.CODE_KEY, command.Reason.Code));
			Assert.True(resultString.ContainsJsonProperty(Reason.DESCRIPTION_KEY, command.Reason.Description));

			Assert.False(resultString.ContainsJsonKey(Envelope.PP_KEY));
			Assert.False(resultString.ContainsJsonKey(Envelope.METADATA_KEY));
			Assert.False(resultString.ContainsJsonKey(Command.RESOURCE_KEY));
		}

		[Fact]
		[Trait("Category", "Serialize")]
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
			Assert.True(resultString.HasValidJsonStackedBrackets());
			Assert.True(resultString.ContainsJsonProperty(Envelope.ID_KEY, message.Id));
			Assert.True(resultString.ContainsJsonProperty(Envelope.FROM_KEY, message.From));
			Assert.True(resultString.ContainsJsonProperty(Envelope.PP_KEY, message.Pp));
			Assert.True(resultString.ContainsJsonProperty(Envelope.TO_KEY, message.To));
			Assert.True(resultString.ContainsJsonProperty(Message.TYPE_KEY, message.Content.GetMediaType()));
			Assert.True(resultString.ContainsJsonKey(Message.CONTENT_KEY));
			Assert.True(resultString.ContainsJsonProperty(Message.CONTENT_KEY, content.Text));
			Assert.True(resultString.ContainsJsonProperty(metadataKey1, metadataValue1));
			Assert.True(resultString.ContainsJsonProperty(metadataKey2, metadataValue2));
		}

		[Fact]
		[Trait("Category", "Serialize")]
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

			Assert.True(resultString.HasValidJsonStackedBrackets());
			Assert.True(resultString.ContainsJsonProperty(Envelope.ID_KEY, message.Id));
			Assert.True(resultString.ContainsJsonProperty(Envelope.FROM_KEY, message.From));
			Assert.True(resultString.ContainsJsonProperty(Envelope.PP_KEY, message.Pp));
			Assert.True(resultString.ContainsJsonProperty(Envelope.TO_KEY, message.To));
			Assert.True(resultString.ContainsJsonProperty(Message.TYPE_KEY, message.Content.GetMediaType()));
			Assert.True(resultString.ContainsJsonKey(Message.CONTENT_KEY));
			
			foreach (var keyValuePair in content)
			{
				if (keyValuePair.Value.GetType().IsArray)
				{
					// TODO: Verify for array properties
				}
				else if (keyValuePair.Value is IDictionary<string, object>)
				{
					// TODO: Verify for dictionary properties
				}
				else
				{
					Assert.True(resultString.ContainsJsonProperty(keyValuePair.Key, keyValuePair.Value));
				}				
			}            
			
			Assert.True(resultString.ContainsJsonProperty(metadataKey1, metadataValue1));
			Assert.True(resultString.ContainsJsonProperty(metadataKey2, metadataValue2));
		}

		[Fact]
		[Trait("Category", "Serialize")]
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

			Assert.True(resultString.HasValidJsonStackedBrackets());
			Assert.True(resultString.ContainsJsonProperty(Envelope.ID_KEY, message.Id));
			Assert.True(resultString.ContainsJsonProperty(Envelope.FROM_KEY, message.From));
			Assert.True(resultString.ContainsJsonProperty(Envelope.PP_KEY, message.Pp));
			Assert.True(resultString.ContainsJsonProperty(Envelope.TO_KEY, message.To));
			Assert.True(resultString.ContainsJsonProperty(Message.TYPE_KEY, message.Content.GetMediaType()));
			Assert.True(resultString.ContainsJsonKey(Message.CONTENT_KEY));
			Assert.True(resultString.ContainsJsonProperty(Message.CONTENT_KEY, content.Value));           
			Assert.True(resultString.ContainsJsonProperty(metadataKey1, metadataValue1));
			Assert.True(resultString.ContainsJsonProperty(metadataKey2, metadataValue2));
		}

		[Fact]
		[Trait("Category", "Serialize")]
		public void Serialize_FireAndForgetTextMessage_ReturnsValidJsonString()
		{
			var target = GetTarget();

			var content = Dummy.CreateTextContent();
			var message = Dummy.CreateMessage(content);
			message.Id = Guid.Empty;

			var resultString = target.Serialize(message);

			Assert.True(resultString.HasValidJsonStackedBrackets());

			Assert.True(resultString.ContainsJsonProperty(Envelope.FROM_KEY, message.From));
			Assert.True(resultString.ContainsJsonProperty(Envelope.TO_KEY, message.To));
			Assert.True(resultString.ContainsJsonProperty(Message.TYPE_KEY, message.Content.GetMediaType()));
			Assert.True(resultString.ContainsJsonKey(Message.CONTENT_KEY));
			Assert.True(resultString.ContainsJsonProperty(Message.CONTENT_KEY, content.Text));

			Assert.False(resultString.ContainsJsonKey(Envelope.ID_KEY));
			Assert.False(resultString.ContainsJsonKey(Envelope.PP_KEY));
			Assert.False(resultString.ContainsJsonKey(Envelope.METADATA_KEY));
		}

		[Fact]
		[Trait("Category", "Serialize")]
		public void Serialize_ReceivedNotification_ReturnsValidJsonString()
		{
			var target = GetTarget();

			var notification = Dummy.CreateNotification(Event.Received);
			notification.Id = Guid.NewGuid();
			notification.Pp = Dummy.CreateNode();

			var metadataKey1 = "randomString1";
			var metadataValue1 = Dummy.CreateRandomString(50);
			var metadataKey2 = "randomString2";
			var metadataValue2 = Dummy.CreateRandomString(50);
			notification.Metadata = new Dictionary<string, string>();
			notification.Metadata.Add(metadataKey1, metadataValue1);
			notification.Metadata.Add(metadataKey2, metadataValue2);

			var resultString = target.Serialize(notification);

			Assert.True(resultString.HasValidJsonStackedBrackets());
			Assert.True(resultString.ContainsJsonProperty(Envelope.ID_KEY, notification.Id));
			Assert.True(resultString.ContainsJsonProperty(Envelope.FROM_KEY, notification.From));
			Assert.True(resultString.ContainsJsonProperty(Envelope.PP_KEY, notification.Pp));
			Assert.True(resultString.ContainsJsonProperty(Envelope.TO_KEY, notification.To));
			Assert.True(resultString.ContainsJsonProperty(Notification.EVENT_KEY, notification.Event));
			Assert.True(resultString.ContainsJsonProperty(metadataKey1, metadataValue1));
			Assert.True(resultString.ContainsJsonProperty(metadataKey2, metadataValue2));

			Assert.False(resultString.ContainsJsonKey(Notification.REASON_KEY));
		}

		[Fact]
		[Trait("Category", "Serialize")]
		public void Serialize_FailedNotification_ReturnsValidJsonString()
		{
			var target = GetTarget();

			var notification = Dummy.CreateNotification(Event.Failed);
			notification.Id = Guid.NewGuid();
			notification.Reason = Dummy.CreateReason();

			var resultString = target.Serialize(notification);

			Assert.True(resultString.HasValidJsonStackedBrackets());
			Assert.True(resultString.ContainsJsonProperty(Envelope.ID_KEY, notification.Id));
			Assert.True(resultString.ContainsJsonProperty(Envelope.FROM_KEY, notification.From));
			Assert.True(resultString.ContainsJsonProperty(Envelope.TO_KEY, notification.To));
			Assert.True(resultString.ContainsJsonProperty(Notification.EVENT_KEY, notification.Event));
			Assert.True(resultString.ContainsJsonProperty(Reason.CODE_KEY, notification.Reason.Code));
			Assert.True(resultString.ContainsJsonProperty(Reason.DESCRIPTION_KEY, notification.Reason.Description));

			Assert.False(resultString.ContainsJsonKey(Envelope.PP_KEY));
			Assert.False(resultString.ContainsJsonKey(Envelope.METADATA_KEY));
		}

		[Fact]
		[Trait("Category", "Serialize")]
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

			Assert.True(resultString.HasValidJsonStackedBrackets());
			Assert.True(resultString.ContainsJsonProperty(Envelope.ID_KEY, session.Id));
			Assert.True(resultString.ContainsJsonProperty(Envelope.FROM_KEY, session.From));
			Assert.True(resultString.ContainsJsonProperty(Envelope.TO_KEY, session.To));
			Assert.True(resultString.ContainsJsonProperty(Session.STATE_KEY, session.State));
			Assert.True(resultString.ContainsJsonProperty(metadataKey1, metadataValue1));
			Assert.True(resultString.ContainsJsonProperty(metadataKey2, metadataValue2));
			Assert.True(resultString.ContainsJsonKey(Session.AUTHENTICATION_KEY));
			Assert.True(resultString.ContainsJsonProperty(PlainAuthentication.PASSWORD_KEY, plainAuthentication.Password));

			Assert.False(resultString.ContainsJsonKey(Envelope.PP_KEY));
			Assert.False(resultString.ContainsJsonKey(Session.REASON_KEY));
		}

		[Fact]
		[Trait("Category", "Serialize")]
		public void Serialize_FailedSession_ReturnsValidJsonString()
		{
			var target = GetTarget();

			var session = Dummy.CreateSession();
			session.State = SessionState.Failed;
			session.Reason = Dummy.CreateReason();

			var resultString = target.Serialize(session);

			Assert.True(resultString.HasValidJsonStackedBrackets());
			Assert.True(resultString.ContainsJsonProperty(Envelope.ID_KEY, session.Id));
			Assert.True(resultString.ContainsJsonProperty(Envelope.FROM_KEY, session.From));
			Assert.True(resultString.ContainsJsonProperty(Envelope.TO_KEY, session.To));
			Assert.True(resultString.ContainsJsonProperty(Session.STATE_KEY, session.State));
			Assert.True(resultString.ContainsJsonKey(Session.REASON_KEY));
			Assert.True(resultString.ContainsJsonProperty(Reason.CODE_KEY, session.Reason.Code));
			Assert.True(resultString.ContainsJsonProperty(Reason.DESCRIPTION_KEY, session.Reason.Description));

			Assert.False(resultString.ContainsJsonKey(Envelope.PP_KEY));
			Assert.False(resultString.ContainsJsonKey(Envelope.METADATA_KEY));
			Assert.False(resultString.ContainsJsonKey(Session.AUTHENTICATION_KEY));
		}

		#endregion

		#region Deserialize

		[Fact]
		[Trait("Category", "Deserialize")]
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

			var id = Guid.NewGuid();

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
            Assert.Equal(id, command.Id);
			Assert.Equal(from, command.From);
			Assert.Equal(pp, command.Pp);
			Assert.Equal(to, command.To);

			Assert.Equal(method, command.Method);
			Assert.NotNull(command.Metadata);
			Assert.True(command.Metadata.ContainsKey(randomKey1));
			Assert.Equal(command.Metadata[randomKey1], randomString1);
			Assert.True(command.Metadata.ContainsKey(randomKey2));
			Assert.Equal(command.Metadata[randomKey2], randomString2);
			
		    var capability = command.Resource.ShouldBeOfType<Capability>();
			Assert.True(capability.ContentTypes.Any(c => c.Equals(contentType1)));
			Assert.True(capability.ContentTypes.Any(c => c.Equals(contentType2)));
			Assert.True(capability.ContentTypes.Any(c => c.Equals(contentType3)));

			Assert.True(capability.ResourceTypes.Any(c => c.Equals(resourceType1)));
			Assert.True(capability.ResourceTypes.Any(c => c.Equals(resourceType2)));
			Assert.True(capability.ResourceTypes.Any(c => c.Equals(resourceType3)));

			Assert.NotNull(command.Uri);
			Assert.Equal(command.Uri, resourceUri);
		}

		[Fact]
		[Trait("Category", "Deserialize")]
		public void Deserialize_AccountRequestCommand_ReturnsValidInstance()
		{
			var target = GetTarget();

			var method = CommandMethod.Get;
			var id = Guid.NewGuid();
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


		[Fact]
		[Trait("Category", "Deserialize")]
		public void Deserialize_PresenceRequestCommand_ReturnsValidInstance()
		{
			var target = GetTarget();

			var method = CommandMethod.Get;
			var id = Guid.NewGuid();
			var from = Dummy.CreateNode();
			var to = Dummy.CreateNode();
			var resourceUri = new LimeUri("/presence");
			var status = PresenceStatus.Available;
			var message = Dummy.CreateRandomStringExtended(100);
			var routingRule = RoutingRule.IdentityByDistance;
			var lastSeen = DateTimeOffset.UtcNow;
			var priority = Dummy.CreateRandomInt(100);
			
			string json =
			    $"{{\"uri\":\"{resourceUri}\",\"type\":\"application/vnd.lime.presence+json\",\"resource\":{{\"status\": \"{status.ToString().ToCamelCase()}\",\"message\":\"{message.Escape()}\",\"routingRule\":\"{routingRule.ToString().ToCamelCase()}\",\"lastSeen\":\"{lastSeen.ToUniversalTime().ToString(TextJsonWriter.DATE_FORMAT, CultureInfo.InvariantCulture)}\",\"priority\":{priority}}},\"method\":\"{method.ToString().ToCamelCase()}\",\"id\":\"{id}\",\"from\":\"{@from}\",\"to\":\"{to}\"}}";

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

		[Fact]
		[Trait("Category", "Deserialize")]
		public void Deserialize_AbsoluteUriRequestCommand_ReturnsValidInstance()
		{
			var target = GetTarget();

			var method = CommandMethod.Get;

			var id = Guid.NewGuid();

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
            Assert.Equal(id, command.Id);
			Assert.Equal(from, command.From);
			Assert.Equal(pp, command.Pp);
			Assert.Equal(to, command.To);

			Assert.Equal(method, command.Method);
			Assert.NotNull(command.Metadata);
			Assert.True(command.Metadata.ContainsKey(randomKey1));
			Assert.Equal(command.Metadata[randomKey1], randomString1);
			Assert.True(command.Metadata.ContainsKey(randomKey2));
			Assert.Equal(command.Metadata[randomKey2], randomString2);

			Assert.NotNull(command.Uri);
			Assert.Equal(command.Uri, resourceUri);

			Assert.Null(command.Type);
			Assert.Null(command.Resource);
		}

		[Fact]
		[Trait("Category", "Deserialize")]
		public void Deserialize_ReceiptRequestCommand_ReturnsValidInstance()
		{
			var target = GetTarget();

			var method = CommandMethod.Set;
			var id = Guid.NewGuid();

			string json =
			    $"{{\"type\":\"application/vnd.lime.receipt+json\",\"resource\":{{\"events\":[\"dispatched\",\"received\"]}},\"method\":\"{method.ToString().ToCamelCase()}\",\"id\":\"{id}\"}}";

			var envelope = target.Deserialize(json);

            var command = envelope.ShouldBeOfType<Command>();
            Assert.Equal(id, command.Id);
			Assert.Null(command.From);
			Assert.Null(command.Pp);
			Assert.Null(command.To);

			Assert.Equal(method, command.Method);
			Assert.Null(command.Metadata);
			Assert.Equal(command.Type.ToString(), Receipt.MIME_TYPE);
			Assert.NotNull(command.Resource);
            command.Resource.ShouldBeOfType<Receipt>();
		}

		[Fact]
		[Trait("Category", "Deserialize")]
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

			var id = Guid.NewGuid();

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
            Assert.Equal(id, command.Id);
			Assert.Equal(from, command.From);
			Assert.Equal(pp, command.Pp);
			Assert.Equal(to, command.To);

			Assert.Equal(method, command.Method);
			Assert.NotNull(command.Metadata);
			Assert.True(command.Metadata.ContainsKey(randomKey1));
			Assert.Equal(command.Metadata[randomKey1], randomString1);
			Assert.True(command.Metadata.ContainsKey(randomKey2));
			Assert.Equal(command.Metadata[randomKey2], randomString2);
            
			var documents = command.Resource.ShouldBeOfType<DocumentCollection>();
			Assert.NotNull(documents.Items);
			Assert.Equal(documents.Items.Length, 3);		    

            var contacts = documents.Cast<Contact>().ToArray();

			Assert.True(contacts[0].Identity.Equals(identity1));
			Assert.True(contacts[0].Name.Equals(name1));
			Assert.True(contacts[0].IsPending.HasValue);
			Assert.True(contacts[0].IsPending.Value);
			Assert.True(contacts[0].ShareAccountInfo.HasValue);
			Assert.False(contacts[0].ShareAccountInfo.Value);
			Assert.False(contacts[0].SharePresence.HasValue);

			Assert.True(contacts[1].Identity.Equals(identity2));
			Assert.True(contacts[1].Name.Equals(name2));
			Assert.False(contacts[1].IsPending.HasValue);
			Assert.False(contacts[1].ShareAccountInfo.HasValue);
			Assert.True(contacts[1].SharePresence.HasValue);
			Assert.False(contacts[1].SharePresence.Value);

			Assert.True(contacts[2].Identity.Equals(identity3));
			Assert.True(contacts[2].Name.Equals(name3));
			Assert.True(contacts[2].IsPending.HasValue);
			Assert.True(contacts[2].IsPending.Value);
			Assert.False(contacts[2].ShareAccountInfo.HasValue);
			Assert.True(contacts[2].SharePresence.HasValue);
			Assert.False(contacts[2].SharePresence.Value);			
		}

		[Fact]
		[Trait("Category", "Deserialize")]
		public void Deserialize_FailureCapabilityResponseCommand_ReturnsValidInstance()
		{
			var target = GetTarget();

			var method = CommandMethod.Get;
			var status = CommandStatus.Failure;
			var reason = Dummy.CreateReason();
			var id = Guid.NewGuid();
			var from = Dummy.CreateNode();
			var to = Dummy.CreateNode();

			string json =
			    $"{{\"method\":\"{method.ToString().ToCamelCase()}\",\"id\":\"{id}\",\"from\":\"{@from}\",\"to\":\"{to}\",\"status\":\"{status}\",\"reason\":{{\"code\":{reason.Code},\"description\":\"{reason.Description}\"}}}}";

			var envelope = target.Deserialize(json);			
            var command = envelope.ShouldBeOfType<Command>();
            Assert.Equal(id, command.Id);
			Assert.Equal(from, command.From);
			Assert.Equal(to, command.To);
			Assert.Equal(method, command.Method);

			Assert.Null(command.Pp);
			Assert.Null(command.Metadata);
			Assert.Null(command.Type);
			Assert.Null(command.Resource);

			Assert.NotNull(command.Reason);

			Assert.Equal(reason.Code, command.Reason.Code);
			Assert.Equal(reason.Description, command.Reason.Description);
		}

		[Fact]
		[Trait("Category", "Deserialize")]
		public void Deserialize_TextMessage_ReturnsValidInstance()
		{
			var target = GetTarget();

			var id = Guid.NewGuid();
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
            Assert.Equal(id, message.Id);
			Assert.Equal(from, message.From);
			Assert.Equal(pp, message.Pp);
			Assert.Equal(to, message.To);
			Assert.NotNull(message.Metadata);
			Assert.True(message.Metadata.ContainsKey(randomKey1));
			Assert.Equal(message.Metadata[randomKey1], randomString1);
			Assert.True(message.Metadata.ContainsKey(randomKey2));
			Assert.Equal(message.Metadata[randomKey2], randomString2);

			message.Content.ShouldBeOfType<PlainText>();

			var textContent = (PlainText)message.Content;
			Assert.Equal(text, textContent.Text);
		}

		[Fact]
		[Trait("Category", "Deserialize")]
		public void Deserialize_ChatStateMessage_ReturnsValidInstance()
		{
			var target = GetTarget();

			var id = Guid.NewGuid();
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
            Assert.Equal(id, message.Id);
			Assert.Equal(from, message.From);
			Assert.Equal(pp, message.Pp);
			Assert.Equal(to, message.To);
			Assert.NotNull(message.Metadata);
			Assert.True(message.Metadata.ContainsKey(randomKey1));
			Assert.Equal(message.Metadata[randomKey1], randomString1);
			Assert.True(message.Metadata.ContainsKey(randomKey2));
			Assert.Equal(message.Metadata[randomKey2], randomString2);

			message.Content.ShouldBeOfType<ChatState>();

			var textContent = (ChatState)message.Content;
			Assert.Equal(state, textContent.State);
		}

		[Fact]
		[Trait("Category", "Deserialize")]
		public void Deserialize_UnknownPlainContentMessage_ReturnsValidInstance()
		{
			var target = GetTarget();

			var id = Guid.NewGuid();
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
            Assert.Equal(id, message.Id);
			Assert.Equal(from, message.From);
			Assert.Equal(pp, message.Pp);
			Assert.Equal(to, message.To);
			Assert.NotNull(message.Metadata);
			Assert.True(message.Metadata.ContainsKey(randomKey1));
			Assert.Equal(message.Metadata[randomKey1], randomString1);
			Assert.True(message.Metadata.ContainsKey(randomKey2));
			Assert.Equal(message.Metadata[randomKey2], randomString2);

			Assert.NotNull(message.Type);
			Assert.Equal(message.Type, type);
			
			var content = message.Content.ShouldBeOfType<PlainDocument>();
            Assert.Equal(text, content.Value);

		}

		[Fact]
		[Trait("Category", "Deserialize")]
		public void Deserialize_UnknownJsonContentMessage_ReturnsValidInstance()
		{
			var target = GetTarget();

			var id = Guid.NewGuid();
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
			    $"{{\"type\":\"{type}\",\"content\":{{\"{propertyName1.Escape()}\":\"{propertyValue1.Escape()}\",\"{propertyName2.Escape()}\":{propertyValue2},\"{propertyName3.Escape()}\":[{{\"{arrayPropertyName1.Escape()}\":\"{arrayPropertyValue1}\",\"{arrayPropertyName2.Escape()}\":{arrayPropertyValue2}}},{{\"{arrayPropertyName3.Escape()}\":\"{arrayPropertyValue3.Escape()}\",\"{arrayPropertyName4.Escape()}\":{arrayPropertyValue4.ToString().ToLower()}}}],\"{propertyName4.Escape()}\":\"{propertyValue4.ToUniversalTime().ToString(TextJsonWriter.DATE_FORMAT, CultureInfo.InvariantCulture)}\"}},\"id\":\"{id}\",\"from\":\"{@from}\",\"pp\":\"{pp}\",\"to\":\"{to}\",\"metadata\":{{\"{randomKey1}\":\"{randomString1.Escape()}\",\"{randomKey2}\":\"{randomString2.Escape()}\"}}}}";

			var envelope = target.Deserialize(json);

            var message = envelope.ShouldBeOfType<Message>();
            Assert.Equal(id, message.Id);
			Assert.Equal(from, message.From);
			Assert.Equal(pp, message.Pp);
			Assert.Equal(to, message.To);
			Assert.NotNull(message.Metadata);
			Assert.True(message.Metadata.ContainsKey(randomKey1));
			Assert.Equal(message.Metadata[randomKey1], randomString1);
			Assert.True(message.Metadata.ContainsKey(randomKey2));
			Assert.Equal(message.Metadata[randomKey2], randomString2);

			Assert.NotNull(message.Type);
			Assert.Equal(message.Type, type);
			
			var content = message.Content.ShouldBeOfType<JsonDocument>();

            Assert.True(content.ContainsKey(propertyName1));
			Assert.Equal(content[propertyName1], propertyValue1);
			Assert.True(content.ContainsKey(propertyName2));            
			Assert.Equal(content[propertyName2], propertyValue2);            
			Assert.True(content.ContainsKey(propertyName3));
			Assert.True(content[propertyName3] is IList<object>);

			var list = (IList<object>) content[propertyName3];
			Assert.Equal(2, list.Count);

			for (int i = 0; i < list.Count; i++)
			{
				var item = (IDictionary<string, object>)list[i];
				if (i == 0)
				{
					Assert.True(item.ContainsKey(arrayPropertyName1));
					Assert.Equal(arrayPropertyValue1, item[arrayPropertyName1]);
					Assert.True(item.ContainsKey(arrayPropertyName2));
					Assert.Equal(arrayPropertyValue2, item[arrayPropertyName2]);
				}
				else if (i == 1)
				{
					Assert.True(item.ContainsKey(arrayPropertyName3));
					Assert.Equal(arrayPropertyValue3, item[arrayPropertyName3]);
					Assert.True(item.ContainsKey(arrayPropertyName4));
					Assert.Equal(arrayPropertyValue4, item[arrayPropertyName4]);
				}
			}

			Assert.True(content.ContainsKey(propertyName4));

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

		[Fact]
		[Trait("Category", "Deserialize")]
		public void Deserialize_GenericJsonContentMessage_ReturnsValidInstance()
		{
			var target = GetTarget();

			var id = Guid.NewGuid();
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
            Assert.Equal(id, message.Id);
			Assert.Equal(from, message.From);
			Assert.Equal(pp, message.Pp);
			Assert.Equal(to, message.To);
			Assert.NotNull(message.Metadata);
			Assert.True(message.Metadata.ContainsKey(randomKey1));
			Assert.Equal(message.Metadata[randomKey1], randomString1);
			Assert.True(message.Metadata.ContainsKey(randomKey2));
			Assert.Equal(message.Metadata[randomKey2], randomString2);

			Assert.NotNull(message.Type);
			Assert.Equal(message.Type, type);
			
			var content = message.Content.ShouldBeOfType<JsonDocument>();

            Assert.True(content.ContainsKey(propertyName1));
			Assert.Equal(content[propertyName1], propertyValue1);
			Assert.True(content.ContainsKey(propertyName2));
			Assert.Equal(content[propertyName2], propertyValue2);

		}

		[Fact]
		[Trait("Category", "Deserialize")]
		public void Deserialize_FireAndForgetTextMessage_ReturnsValidInstance()
		{
			var target = GetTarget();

			var from = Dummy.CreateNode();
			var to = Dummy.CreateNode();

			var text = Dummy.CreateRandomStringExtended(50);

			string json =
			    $"{{\"type\":\"text/plain\",\"content\":\"{text.Escape()}\",\"from\":\"{@from}\",\"to\":\"{to}\"}}";

			var envelope = target.Deserialize(json);

            var message = envelope.ShouldBeOfType<Message>();
            Assert.Equal(from, message.From);
			Assert.Equal(to, message.To);

			Assert.Equal(message.Id, Guid.Empty);
			Assert.Null(message.Pp);
			Assert.Null(message.Metadata);
			
			var textContent = message.Content.ShouldBeOfType<PlainText>();
			Assert.Equal(text, textContent.Text);
		}

		[Fact]
		[Trait("Category", "Deserialize")]
		public void Deserialize_FireAndForgetChatStateMessage_ReturnsValidInstance()
		{
			var target = GetTarget();

			var from = Dummy.CreateNode();
			var to = Dummy.CreateNode();

			var state = ChatStateEvent.Composing;

			string json =
			    $"{{\"type\":\"application/vnd.lime.chatstate+json\",\"content\":{{\"state\":\"{state.ToString().ToCamelCase()}\"}},\"from\":\"{@from}\",\"to\":\"{to}\"}}";

			var envelope = target.Deserialize(json);

		    var message = envelope.ShouldBeOfType<Message>();
			Assert.Equal(from, message.From);
			Assert.Equal(to, message.To);

			Assert.Equal(message.Id, Guid.Empty);
			Assert.Null(message.Pp);
			Assert.Null(message.Metadata);			

            var textContent = message.Content.ShouldBeOfType<ChatState>();
			Assert.Equal(state, textContent.State);            
		}

		[Fact]
		[Trait("Category", "Deserialize")]
		public void Deserialize_ReceivedNotification_ReturnsValidInstance()
		{
			var target = GetTarget();

			var id = Guid.NewGuid();
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
			Assert.Equal(id, notification.Id);
			Assert.Equal(from, notification.From);
			Assert.Equal(pp, notification.Pp);
			Assert.Equal(to, notification.To);
			Assert.NotNull(notification.Metadata);
			Assert.True(notification.Metadata.ContainsKey(randomKey1));
			Assert.Equal(notification.Metadata[randomKey1], randomString1);
			Assert.True(notification.Metadata.ContainsKey(randomKey2));
			Assert.Equal(notification.Metadata[randomKey2], randomString2);

			Assert.Equal(@event, notification.Event);

			Assert.Null(notification.Reason);
		}

		[Fact]
		[Trait("Category", "Deserialize")]
		public void Deserialize_FailedNotification_ReturnsValidInstance()
		{
			var target = GetTarget();

			var @event = Event.Received;

			var reasonCode = Dummy.CreateRandomInt(100);
			var reasonDescription = Dummy.CreateRandomStringExtended(100);

			var id = Guid.NewGuid();
			var from = Dummy.CreateNode();
			var to = Dummy.CreateNode();

			string json =
			    $"{{\"event\":\"{@event.ToString().ToCamelCase()}\",\"id\":\"{id}\",\"from\":\"{@from}\",\"to\":\"{to}\",\"reason\":{{\"code\":{reasonCode},\"description\":\"{reasonDescription.Escape()}\"}}}}";

			var envelope = target.Deserialize(json);

            var notification = envelope.ShouldBeOfType<Notification>();
            Assert.Equal(id, notification.Id);
			Assert.Equal(from, notification.From);
			Assert.Equal(to, notification.To);
			Assert.Equal(@event, notification.Event);

			Assert.Null(notification.Pp);
			Assert.Null(notification.Metadata);

			Assert.NotNull(notification.Reason);

			Assert.Equal(reasonCode, notification.Reason.Code);
			Assert.Equal(reasonDescription, notification.Reason.Description);
		}

		[Fact]
		[Trait("Category", "Deserialize")]
		public void Deserialize_AuthenticatingSession_ReturnsValidInstance()
		{
			var target = GetTarget();

			var id = Guid.NewGuid();
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
			Assert.Equal(id, session.Id);
			Assert.Equal(from, session.From);
			Assert.Equal(to, session.To);
			Assert.NotNull(session.Metadata);
			Assert.True(session.Metadata.ContainsKey(randomKey1));
			Assert.Equal(session.Metadata[randomKey1], randomString1);
			Assert.True(session.Metadata.ContainsKey(randomKey2));
			Assert.Equal(session.Metadata[randomKey2], randomString2);

			Assert.Equal(state, session.State);

			Assert.Null(session.Pp);
			Assert.Null(session.Reason);
		}

		[Fact]
		[Trait("Category", "Deserialize")]
		public void Deserialize_FailedSessionNullProperties_ReturnsValidInstance()
		{
			var target = GetTarget();

			var id = Guid.NewGuid();
			var from = Dummy.CreateNode();
			var to = Dummy.CreateNode();        
			var state = SessionState.Authenticating;

			var reasonCode = Dummy.CreateRandomInt(100);
			var reasonDescription = Dummy.CreateRandomStringExtended(100);

			string json =
			    $"{{\"state\":\"{state.ToString().ToCamelCase()}\",\"id\":\"{id}\",\"from\":\"{@from}\",\"to\":\"{to}\",\"reason\":{{\"code\":{reasonCode},\"description\":\"{reasonDescription.Escape()}\"}},\"encryptionOptions\":null,\"compressionOptions\":null,\"compression\":null,\"encryption\":null}}";

			var envelope = target.Deserialize(json);

            var session = envelope.ShouldBeOfType<Session>();
            Assert.Equal(id, session.Id);
			Assert.Equal(from, session.From);
			Assert.Equal(to, session.To);

			Assert.Equal(state, session.State);

			Assert.NotNull(session.Reason);
			Assert.Equal(reasonCode, session.Reason.Code);
			Assert.Equal(reasonDescription, session.Reason.Description);

			Assert.Null(session.Pp);
			Assert.Null(session.Metadata);
		}

		[Fact]
		[Trait("Category", "Deserialize")]
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

		[Fact]
		[Trait("Category", "Deserialize")]
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

		[Fact]
		[Trait("Category", "Deserialize")]
		public void Deserialize_RandomResourceRequestCommand_ReturnsValidInstance()
		{
			var target = GetTarget();

			var method = CommandMethod.Set;
			var id = Guid.NewGuid();

			string json =
			    $"{{\"type\":\"application/vnd.takenet.testdocument+json\",\"resource\":{{\"double\":10.1, \"NullableDouble\": 10.2}},\"method\":\"{method.ToString().ToCamelCase()}\",\"id\":\"{id}\"}}";

			var envelope = target.Deserialize(json);

			var command = envelope.ShouldBeOfType<Command>();
			command.Type.ToString().ShouldBe(TestDocument.MIME_TYPE);
			command.Resource.ShouldNotBe(null);
			var document = command.Resource.ShouldBeOfType<TestDocument>();
			document.Double.ShouldBe(10.1d);
			document.NullableDouble.ShouldBe(10.2d);
		}


		#endregion
	}
}