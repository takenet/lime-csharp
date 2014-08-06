using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Resources
{
    /// <summary>
    /// Allows the nodes to manage the groups that they owns or participates.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public partial class Group : Document
    {
        public const string MIME_TYPE = "application/vnd.lime.group+json";

        public const string IDENTITY_KEY = "identity";
        public const string NAME_KEY = "name";
        public const string TYPE_KEY = "type";
        public const string MEMBERS_KEY = "members";


        public Group()
            : base(MediaType.Parse(MIME_TYPE))
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
        /// Type of the group.
        /// </summary>
        [DataMember(Name = TYPE_KEY)]
        public GroupType? Type { get; set; }

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

    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    [DebuggerDisplay("Identity = {Identity}, Role = {Role}")]
    public partial class GroupMember : Document
    {


        public const string MIME_TYPE = "application/vnd.lime.groupmember+json";

        public GroupMember()
            : base(MediaType.Parse(MIME_TYPE))
        {

        }
        /// <summary>
        /// The identity of the member, in the name@domain format. 
        /// </summary>
        [DataMember(Name = "identity")]
        public Identity Identity { get; set; }

        /// <summary>
        /// The role of the identity in the group. 
        /// </summary>
        [DataMember(Name = "role")]
        public GroupMemberRole Role { get; set; }

    }

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
