using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol
{
    /// <summary>
    /// Represents an element in a network
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org")]
    public class Identity : IIdentity
    {
        /// <summary>
        /// Identity unique name 
        /// on his domain
        /// </summary>
        [DataMember(Name = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Network domain name 
        /// of the identity
        /// </summary>
        [DataMember(Name = "domain")]
        public string Domain { get; set; }

        public override string ToString()
        {
            return string.Format("{0}@{1}", Name, Domain);
        }

        public override int GetHashCode()
        {
            return ToString().ToLower().GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return ToString().Equals(obj.ToString());
        }

        public static Identity Parse(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                throw new ArgumentNullException("s");
            }

            var splittedIdentity = s.Split('@');

            if (splittedIdentity.Length != 2)
            {
                throw new FormatException("Invalid identity format");
            }

            return new Identity()
            {
                Name = splittedIdentity[0],
                Domain = splittedIdentity[1].Split('/')[0]
            };
        }

        public static bool TryParse(string s, out Identity value)
        {
            try
            {
                value = Parse(s);
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
