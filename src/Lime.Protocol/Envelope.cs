using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol
{
    /// <summary>
    /// Base class to all 
    /// communication documents
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public abstract class Envelope
    {
        #region Constructor

        public Envelope()
            : this(Guid.NewGuid())
        {
        }

        protected Envelope(Guid? id)
        {
            Id = id;
        }

        #endregion

        /// <summary>
        /// Unique identifier of the envelope
        /// </summary>
        [DataMember(Name = "id")]
        public Guid? Id { get; set; }

        /// <summary>
        /// Sender of the envelope
        /// </summary>
        [DataMember(Name = "from")]
        public Node From { get; set; }

        /// <summary>
        /// Sender of the envelope
        /// </summary>
        [DataMember(Name = "pp")]
        public Node Pp { get; set; }

        /// <summary>
        /// Destination of the envelope
        /// </summary>
        [DataMember(Name = "to")]
        public Node To { get; set; }

        /// <summary>
        /// Additional information to be 
        /// delivered with the envelope
        /// </summary>
        [DataMember(Name = "metadata")]
        public IDictionary<string, object> Metadata { get; set; }        
    }
}