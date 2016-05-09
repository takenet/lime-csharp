using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Lime.Protocol
{
    /// <summary>
    /// Base class to all communication documents.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public abstract class Envelope
    {
        public const string ID_KEY = "id";
        public const string FROM_KEY = "from";
        public const string PP_KEY = "pp";
        public const string TO_KEY = "to";
        public const string METADATA_KEY = "metadata";

        protected Envelope()
            : this(null)
        {

        }

        protected Envelope(string id)
        {
            Id = id;
        }

        /// <summary>
        /// Gets or sets the identifier of the envelope.
        /// </summary>
        [DataMember(Name = ID_KEY)]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the sender node of the envelope. 
        /// If a node receives an envelope without this value, it means that the envelope was originated by the remote party.
        /// </summary>
        [DataMember(Name = FROM_KEY)]
        public Node From { get; set; }

        /// <summary>
        ///  Gets or sets the delegation node. Its an acronym for per procurationem. 
        /// Identifier of a delegate node (a node that received a permission to send on behalf of another). 
        /// Allows a node to send an envelope on behalf of another identity.
        /// </summary>
        [DataMember(Name = PP_KEY)]
        public Node Pp { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the destination node of the envelope. 
        /// If a node receives an envelope without this value, it means that the envelope is addressed to itself.
        /// </summary>
        [DataMember(Name = TO_KEY)]
        public Node To { get; set; }

        /// <summary>
        /// Additional information to be delivered with the envelope.
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