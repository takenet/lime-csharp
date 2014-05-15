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
    public partial class Reason
    {
        public const string CODE_KEY = "code";
        public const string DESCRIPTION_KEY = "description";

        [DataMember(Name = CODE_KEY)]
        public int Code { get; set; }

        [DataMember(Name = DESCRIPTION_KEY)]
        public string Description { get; set; }

        public override string ToString()
        {
            return string.Format("{0} (Code {1})", Description, Code);
        }
    }
}
