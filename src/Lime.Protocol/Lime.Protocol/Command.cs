using Lime.Protocol.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol
{
    /// <summary>
    /// Represents peer-to-server interaction, 
    /// like a information query or parameters
    /// definition change
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public partial class Command : Envelope
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
                if (this.Resource != null)
                {
                    return this.Resource.GetMediaType();
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
    /// Defines method for the manipulation 
    /// of resources.
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
        /// Sets or updates a for the resource.
        /// </summary>
        [EnumMember(Value = "set")]
        Set,
        /// <summary>
        /// Deletes a value of the resource 
        /// or the resource itself.
        /// </summary>
        [EnumMember(Value = "delete")]
        Delete,
        /// <summary>
        /// Notify the destination about a change 
        /// in the resource value of the sender. 
        /// This method is one way and the destination 
        /// SHOULD NOT send a response for it. 
        /// Because of that, a command envelope with this 
        /// method MAY NOT have an id.
        /// </summary>
        [EnumMember(Value = "observe")]
        Observe
    }

    /// <summary>
    /// Represents the status
    /// of a resource operation
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public enum CommandStatus
    {
        /// <summary>
        /// The resource action is pending
        /// </summary>
        [EnumMember(Value = "pending")]
        Pending,
        /// <summary>
        /// The resource action was 
        /// sucessfully
        /// </summary>
        [EnumMember(Value = "success")]
        Success,
        /// <summary>
        /// 
        /// </summary>
        [EnumMember(Value = "failure")]
        Failure
    }
}