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
    public class Node : Identity
    {
        /// <summary>
        /// The name of the instance used
        /// by the node to connect to the network
        /// </summary>
        public string Instance { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("{0}/{1}", base.ToString(), Instance).TrimEnd('/');
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" }, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            Node node = obj as Node;

            if (node == null)
            {
                return false;
            }

            return ((this.Name == null && node.Name == null) || (this.Name != null && this.Name.Equals(node.Name, StringComparison.CurrentCultureIgnoreCase))) &&
                   ((this.Domain == null && node.Domain == null) || (this.Domain != null && this.Domain.Equals(node.Domain, StringComparison.CurrentCultureIgnoreCase))) &&
                   ((this.Instance == null && node.Instance == null) || (this.Instance != null && this.Instance.Equals(node.Instance, StringComparison.CurrentCultureIgnoreCase)));
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Parses the string to a valid Node.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">s</exception>
        /// <exception cref="System.FormatException">Invalid Peer format</exception>
        public static new Node Parse(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                throw new ArgumentNullException("s");
            }

            var identity = Identity.Parse(s);

            var splittedDomain = identity.Domain != null ? identity.Domain.Split('/') : null;

            return new Node()
            {
                Name = identity.Name,
                Domain = splittedDomain != null ? splittedDomain[0] : null,
                Instance = splittedDomain != null && splittedDomain.Length > 1 ? splittedDomain[1] : null
            };
        }

        /// <summary>
        /// Tries to parse the string to a valid Node
        /// </summary>
        /// <param name="s">The s.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static bool TryParse(string s, out Node value)
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

        /// <summary>
        /// Creates an Identity instance
        /// based on the Node identity
        /// </summary>
        /// <returns></returns>
        public Identity ToIdentity()
        {
            return new Identity()
            {
                Name = this.Name,
                Domain = this.Domain
            };
        }

        /// <summary>
        /// Indicates if the node is
        /// a complete representation, with
        /// name, domain and instance.
        /// </summary>
        public bool IsComplete
        {
            get
            {
                return
                    !string.IsNullOrEmpty(Name) &&
                    !string.IsNullOrEmpty(Domain) &&
                    !string.IsNullOrEmpty(Instance);
            }
        }

        #region ICloneable Members

        /// <summary>
        /// Creates a new object that 
        /// is a copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new object that is a copy of this instance.
        /// </returns>
        public Node Copy()
        {
            return new Node()
            {
                Name = this.Name,
                Domain = this.Domain,
                Instance = this.Instance
            };
        }

        #endregion
    }
}