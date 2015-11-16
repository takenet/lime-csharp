using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Lime.Protocol
{
    /// <summary>
    /// Base class to all 
    /// communication documents
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public abstract class Envelope
    {
        public const string ID_KEY = "id";
        public const string FROM_KEY = "from";
        public const string PP_KEY = "pp";
        public const string TO_KEY = "to";
        public const string METADATA_KEY = "metadata";

        #region Constructor

        protected Envelope()
            : this(Guid.NewGuid())
        {
        }

        protected Envelope(Guid id)
        {
            Id = id;
        }

        #endregion

        /// <summary>
        /// Unique identifier of the envelope
        /// </summary>
        [DataMember(Name = ID_KEY, EmitDefaultValue = false)]
        public Guid Id { get; set; }

        /// <summary>
        /// Sender of the envelope
        /// </summary>
        [DataMember(Name = FROM_KEY)]
        public Node From { get; set; }

        /// <summary>
        /// Sender of the envelope
        /// </summary>
        [DataMember(Name = PP_KEY)]
        public Node Pp { get; set; }

        /// <summary>
        /// Destination of the envelope
        /// </summary>
        [DataMember(Name = TO_KEY)]
        public Node To { get; set; }

        /// <summary>
        /// Additional information to be 
        /// delivered with the envelope
        /// </summary>
        [DataMember(Name = METADATA_KEY)]
        public IDictionary<string, string> Metadata { get; set; }

        /// <summary>
        /// Creates a shallow copy of the current <see cref="Envelope"/>.
        /// </summary>
        /// <returns></returns>
        internal new Envelope MemberwiseClone()
        {
            return (Envelope)base.MemberwiseClone();
        }
    }
}