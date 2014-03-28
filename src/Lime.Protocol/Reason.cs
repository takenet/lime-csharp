using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol
{
    /// <summary>
    /// Represents a known reason for
    /// events occurred during the client-server 
    /// interactions.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class Reason
    {
        [DataMember(Name = "code")]
        public int Code { get; set; }

        [DataMember(Name = "description")]
        public string Description { get; set; }
    }
}
