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
    [KnownType(typeof(Account))]
    [KnownType(typeof(Capability))]
    //[KnownType(typeof(Presence))]
    //[KnownType(typeof(Roster))]
    //[KnownType(typeof(Subscription))]
    public class Command : Envelope
    {
        /// <summary>
        ///  MIME declaration of the resource type of the command.
        /// </summary>
        [DataMember(Name = "type")]
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
        [DataMember(Name = "resource")]
        public Document Resource { get; set; }

        /// <summary>
        /// Action to be taken to the
        /// resource
        /// </summary>
        [DataMember(Name = "method")]
        public CommandMethod Method { get; set; }

        /// <summary>
        /// Indicates the status of 
        /// the action taken to the resource
        /// </summary>
        [DataMember(Name = "status", EmitDefaultValue = false)]
        [DefaultValue(CommandStatus.Pending)]
        public CommandStatus Status { get; set; }

        /// <summary>
        /// Indicates a reason for
        /// the status
        /// </summary>
        [DataMember(Name = "reason")]
        public Reason Reason { get; set; }
    }

    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public enum CommandMethod
    {
        [EnumMember(Value = "get")]
        Get,
        [EnumMember(Value = "set")]
        Set,
        [EnumMember(Value = "delete")]
        Delete,
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