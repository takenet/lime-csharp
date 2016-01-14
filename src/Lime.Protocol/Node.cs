using System;

namespace Lime.Protocol
{
    /// <summary>
    /// Represents an element of a network.
    /// </summary>
    public class Node : Identity
    {
        #region Constructor

        public Node()
        {

        }

        public Node(string name, string domain, string instance)
            : base(name, domain)            
        {
            Instance = instance;
        }

        #endregion

        /// <summary>
        /// The name of the instance used by the node to connect to the network.
        /// </summary>
        public string Instance { get; set; }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("{0}/{1}", base.ToString(), Instance).TrimEnd('/');
        }

        /// <summary>
        /// Determines whether the specified <see cref="object" }, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            var node = obj as Node;

            if (node == null)
            {
                return false;
            }

            return ((Name == null && node.Name == null) || (Name != null && Name.Equals(node.Name, StringComparison.CurrentCultureIgnoreCase))) &&
                   ((Domain == null && node.Domain == null) || (Domain != null && Domain.Equals(node.Domain, StringComparison.CurrentCultureIgnoreCase))) &&
                   ((Instance == null && node.Instance == null) || (Instance != null && Instance.Equals(node.Instance, StringComparison.CurrentCultureIgnoreCase)));
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

            return new Node
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
        /// Creates an Identity instance based on the Node identity.
        /// </summary>
        /// <returns></returns>
        public Identity ToIdentity()
        {
            return new Identity
            {
                Name = Name,
                Domain = Domain
            };
        }

        /// <summary>
        /// Indicates if the node is a complete representation, 
        /// with name, domain and instance.
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
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new object that is a copy of this instance.
        /// </returns>
        public Node Copy()
        {
            return new Node
            {
                Name = Name,
                Domain = Domain,
                Instance = Instance
            };
        }

        #endregion
    }
}