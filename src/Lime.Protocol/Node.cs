using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol
{
    /// <summary>
    /// Represents an element 
    /// of a network
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org")]
    public class Node : Identity
    {
        /// <summary>
        /// The name of the instance used
        /// by the node to connect to the network
        /// </summary>
        [DataMember(Name = "instance")]
        public string Instance { get; set; }

        public override string ToString()
        {
            return string.Format("{0}/{1}", base.ToString(), Instance).TrimEnd('/');
        }

        public static Node ParseNode(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                throw new ArgumentNullException("s");
            }

            var splittedIdentity = s.Split('@');

            if (splittedIdentity.Length != 2)
            {
                throw new FormatException("Invalid Peer format");
            }

            var splittedDomain = splittedIdentity[1].Split('/');

            string instance = null;
            if (splittedDomain.Length >= 2)
            {
                instance = splittedDomain[1];
            }

            return new Node()
            {
                Name = splittedIdentity[0],
                Domain = splittedDomain[0],
                Instance = instance
            };
        }

        public static bool TryParse(string s, out Node value)
        {
            try
            {
                value = ParseNode(s);
                return true;
            }
            catch
            {
                value = null;
                return false;
            }
        }
    }
}