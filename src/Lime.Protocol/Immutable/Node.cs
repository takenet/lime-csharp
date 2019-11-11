using System;

namespace Lime.Protocol.Immutable
{
    /// <summary>
    /// Represents an element of a network.
    /// </summary>
    public class Node : Identity, INode
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Node"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="domain">The domain.</param>
        /// <param name="instance">The instance.</param>
        public Node(string name, string domain, string instance)
            : base(name, domain)            
        {
            Instance = instance;
        }


        /// <summary>
        /// The name of the instance used by the node to connect to the network.
        /// </summary>
        public string Instance { get; }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            if (Instance == null)
            {
                return base.ToString();
            }
            
            return $"{base.ToString()}/{Instance}";
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
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(Node left, Node right) => Equals(left, right);

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(Node left, Node right) => !Equals(left, right);

        /// <summary>
        /// Performs an implicit conversion from <see cref="System.String"/> to <see cref="Node"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        /// <summary>
        /// Performs an implicit conversion from <see cref="System.String"/> to <see cref="Node"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator Node(string value) => value == null ? null : Parse(value);

        /// <summary>
        /// Performs an implicit conversion from <see cref="Node"/> to <see cref="System.String"/>.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator string(Node node) => node?.ToString();

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode() => base.GetHashCode();

        /// <summary>
        /// Parses the string to a valid Node.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">s</exception>
        /// <exception cref="System.FormatException">Invalid Peer format</exception>
        public new static Node Parse(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                throw new ArgumentNullException(nameof(s));
            }

            var identity = Identity.Parse(s);
            var identityString = identity.ToString();

            return new Node(
                identity.Name,
                identity.Domain,
                s.Length > identityString.Length ? s.Remove(0, identityString.Length + 1) : null);
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
            return new Identity(Name, Domain);
        }

        /// <summary>
        /// Indicates if the node is a complete representation, 
        /// with name, domain and instance.
        /// </summary>
        public bool IsComplete => !string.IsNullOrEmpty(Name) &&
                                  !string.IsNullOrEmpty(Domain) &&
                                  !string.IsNullOrEmpty(Instance);

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new object that is a copy of this instance.
        /// </returns>
        public Node Copy(string name = null, string domain = null, string instance = null)
        {
            return new Node(name ?? Name, domain ?? Domain, instance ?? Instance);
        }
        
        /// <summary>
        /// Create a new instance of <see cref="Lime.Protocol.Node"/> which is mutable based on this instance.
        /// </summary>
        /// <returns></returns>
        public Lime.Protocol.Node ToMutableNode()
        {
            return new Lime.Protocol.Node(Name, Domain, Instance);
        }
    }
}