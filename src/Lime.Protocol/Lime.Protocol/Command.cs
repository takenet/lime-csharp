using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Lime.Protocol
{
    /// <summary>
    /// Allows the manipulation of node resources, like server session parameters or information related to the network nodes.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class Command : Envelope
    {
        public const string URI_KEY = "uri";
        public const string TYPE_KEY = Message.TYPE_KEY;
        public const string RESOURCE_KEY = "resource";
        public const string METHOD_KEY = "method";
        public const string STATUS_KEY = "status";
        public const string REASON_KEY = "reason";

        #region Constructor

        public Command()
        {
        }

        public Command(Guid id)
            : base(id)
        {
        }

        #endregion

        /// <summary>
        /// The universal identifier
        /// of the resource
        /// </summary>
        [DataMember(Name = URI_KEY)]
        public LimeUri Uri { get; set; }

        /// <summary>
        ///  MIME declaration of the resource type of the command.
        /// </summary>
        [DataMember(Name = TYPE_KEY)]
        public MediaType Type
        {
            get
            {
                if (Resource != null)
                {
                    return Resource.GetMediaType();
                }

                return null;
            }
        }

        /// <summary>
        /// Server resource that are subject
        /// of the command
        /// </summary>
        [DataMember(Name = RESOURCE_KEY)]
        public Document Resource { get; set; }

        /// <summary>
        /// Action to be taken to the
        /// resource
        /// </summary>
        [DataMember(Name = METHOD_KEY, EmitDefaultValue = true)]
        public CommandMethod Method { get; set; }

        /// <summary>
        /// Indicates the status of 
        /// the action taken to the resource
        /// </summary>
        [DataMember(Name = STATUS_KEY, EmitDefaultValue = false)]
        [DefaultValue(CommandStatus.Pending)]
        public CommandStatus Status { get; set; }

        /// <summary>
        /// Indicates a reason for
        /// the status
        /// </summary>
        [DataMember(Name = REASON_KEY)]
        public Reason Reason { get; set; }
    }

    /// <summary>
    /// Defines methods for the manipulation of resources.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public enum CommandMethod
    {
        /// <summary>
        /// Gets an existing value of the resource.
        /// </summary>
        [EnumMember(Value = "get")]
        Get,

        /// <summary>
        /// Creates or updates the value of the resource.
        /// </summary>
        [EnumMember(Value = "set")]
        Set,

        /// <summary>
        /// Deletes a value of the resource or the resource itself.
        /// </summary>
        [EnumMember(Value = "delete")]
        Delete,

        /// <summary>
        /// Subscribes to the resource, allowing the originator to be notified when the value of the resource changes in the destination.
        /// </summary>
        [EnumMember(Value = "subscribe")]
        Subscribe,

        /// <summary>
        /// Unsubscribes to the resource, signaling to the destination that the originator do not want to receive further notifications about the resource.
        /// </summary>
        [EnumMember(Value = "unsubscribe")]
        Unsubscribe,

        /// <summary>
        /// Notify the destination about a change in the resource value of the sender. 
        /// If the resource value is absent, it represent that the resource in the specified URI was deleted in the originator.
        /// This method is one way and the destination  SHOULD NOT send a response for it. 
        /// Because of that, a command envelope with this method MAY NOT have an id.
        /// </summary>
        [EnumMember(Value = "observe")]
        Observe,
    }

    /// <summary>
    /// Represents the status
    /// of a resource operation
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public enum CommandStatus
    {
        /// <summary>
        /// The resource action is pending.
        /// </summary>
        [EnumMember(Value = "pending")]
        Pending,

        /// <summary>
        /// The resource action was successfully executed.
        /// </summary>
        [EnumMember(Value = "success")]
        Success,

        /// <summary>
        /// The resource action has failed.
        /// </summary>
        [EnumMember(Value = "failure")]
        Failure
    }
}