using System.Diagnostics;
using System.Runtime.Serialization;
using Lime.Protocol;
using System;

namespace Lime.Messaging.Resources
{
    /// <summary>
    /// Allows the nodes to manage the groups that they owns or participates.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class Group : Document
    {
        public const string MIME_TYPE = "application/vnd.lime.group+json";
        public static readonly MediaType MediaType = MediaType.Parse(MIME_TYPE);

        public const string IDENTITY_KEY = "identity";
        public const string NAME_KEY = "name";
        public const string DESCRIPTION_KEY = "description";
        public const string TYPE_KEY = "type";
        public const string PHOTO_URI_KEY = "photoUri";
        public const string CREATOR_KEY = "creator";
        public const string CREATED_KEY = "created";
        public const string MEMBERS_KEY = "members";

        /// <summary>
        /// Initializes a new instance of the <see cref="Group"/> class.
        /// </summary>
        public Group()
            : base(MediaType)
        {
        }

        /// <summary>
        /// Identity of the group, in the group-id@groups.domain.com format. 
        /// </summary>
        [DataMember(Name = IDENTITY_KEY)]
        public Identity Identity { get; set; }

        /// <summary>
        /// Name of the group.
        /// </summary>
        [DataMember(Name = NAME_KEY)]
        public string Name { get; set; }

        /// <summary>
        /// Description of the group.
        /// </summary>
        [DataMember(Name = DESCRIPTION_KEY)]
        public string Description { get; set; }

        /// <summary>
        /// Type of the group.
        /// </summary>
        [DataMember(Name = TYPE_KEY)]
        public GroupType? Type { get; set; }

        /// <summary>
        /// The group photo URI.
        /// </summary>
        [DataMember(Name = PHOTO_URI_KEY)]
        public Uri PhotoUri { get; set; }

        /// <summary>
        /// The group creator identity.
        /// </summary>
        [DataMember(Name = CREATOR_KEY)]
        public Identity Creator { get; set; }

        /// <summary>
        /// The group creation date.
        /// </summary>
        [DataMember(Name = CREATED_KEY)]
        public DateTime Created { get; set; }

        /// <summary>
        /// Members uri of the contact group. 
        /// </summary>
        [DataMember(Name = MEMBERS_KEY)]
        public LimeUri MembersUri { get; set; }
    }

    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public enum GroupType
    {
        /// <summary>
        /// The group exists while the owner session that created it is active. 
        /// This type of group is useful for mass message sending, since the groups 
        /// application is optimized to send large amounts of messages. 
        /// The temporary groups are private.
        /// </summary>
        [EnumMember(Value = "temporary")]
        Temporary,

        /// <summary>
        /// The group is not discoverable and someone 
        /// can join only if is invited by owner or a moderator.
        /// </summary>
        [EnumMember(Value = "private")]
        Private,

        /// <summary>
        /// Any authenticated node in the domain can join the group.
        /// </summary>
        [EnumMember(Value = "public")]
        Public
    }

    /// <summary>
    /// Represents a member of a group.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    [DebuggerDisplay("Address = {Address}, Role = {Role}")]
    public class GroupMember : Document
    {
        public const string ADDRESS_KEY = "address";
        public const string ROLE_KEY = "role";

        public const string MIME_TYPE = "application/vnd.lime.groupmember+json";

        /// <summary>
        /// Initializes a new instance of the <see cref="GroupMember"/> class.
        /// </summary>
        public GroupMember()
            : base(MediaType.Parse(MIME_TYPE))
        {
        }

        /// <summary>
        /// The address of the member, in the name@domain/instance format,
        /// being the instance information optional.
        /// </summary>
        [DataMember(Name = ADDRESS_KEY)]
        public Node Address { get; set; }

        /// <summary>
        /// The role of the identity in the group. 
        /// </summary>
        [DataMember(Name = ROLE_KEY)]
        public GroupMemberRole Role { get; set; }
    }

    /// <summary>
    /// Enumerates the roles that the members 
    /// can have in a group.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public enum GroupMemberRole
    {
        /// <summary>
        /// The member can send and receive 
        /// messages to the group. 
        /// It's the default value.
        /// </summary>
        [EnumMember(Value = "member")]
        Member,

        /// <summary>
        /// The member can only receive messages 
        /// from the group, and doesn't have permission to send.
        /// </summary>
        [EnumMember(Value = "listener")]
        Listener,

        /// <summary>
        /// The member can send and receive messages to 
        /// the group and can kick and 
        /// ban contacts from it.
        /// </summary>
        [EnumMember(Value = "moderator")]
        Moderator,

        /// <summary>
        /// The owner have the permission to manage moderators, 
        /// change and delete the group.
        /// </summary>
        [EnumMember(Value = "owner")]
        Owner
    }
}