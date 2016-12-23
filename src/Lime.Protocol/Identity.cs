using System;

namespace Lime.Protocol
{
    /// <summary>
    /// Represents an identity in a domain.
    /// </summary>
    public class Identity : IIdentity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Identity"/> class.
        /// </summary>
        public Identity()
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Identity"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="domain">The domain.</param>
        public Identity(string name, string domain)
        {
            Name = name;
            Domain = domain;
        }

        /// <summary>
        /// Identity unique name on his domain.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Network domain name of the identity.
        /// </summary>
        public string Domain { get; set; }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            if (string.IsNullOrWhiteSpace(Domain))
            {
                return Name;
            }
            return $"{Name}@{Domain}";
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return ToString().ToLower().GetHashCode();
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
            var identity = obj as Identity;

            if (identity == null)
            {
                return false;
            }

            return ((Name == null && identity.Name == null) || (Name != null && Name.Equals(identity.Name, StringComparison.CurrentCultureIgnoreCase))) &&
                   ((Domain == null && identity.Domain == null) || (Domain != null && Domain.Equals(identity.Domain, StringComparison.CurrentCultureIgnoreCase)));
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(Identity left, Identity right) => Equals(left, right);

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(Identity left, Identity right) => !Equals(left, right);

        /// <summary>
        /// Performs an implicit conversion from <see cref="System.String"/> to <see cref="Identity"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        /// <summary>
        /// Performs an implicit conversion from <see cref="System.String"/> to <see cref="Identity"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator Identity(string value) => Parse(value);

        /// <summary>
        /// Performs an implicit conversion from <see cref="Identity"/> to <see cref="System.String"/>.
        /// </summary>
        /// <param name="identity">The identity.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator string(Identity identity) => identity.ToString();

        /// <summary>
        /// Creates a Node instance based on the identity,
        /// with a null value for the instance property.
        /// </summary>
        /// <returns></returns>
        public Node ToNode()
        {
            return new Node
            {
                Name = Name,
                Domain = Domain
            };
        }

        /// <summary>
        /// Parses the string to a valid Identity.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">s</exception>
        /// <exception cref="System.FormatException">Invalid identity format</exception>
        public static Identity Parse(string s)
        {
            if (s == null) throw new ArgumentNullException(nameof(s));
            if (s == string.Empty) throw new ArgumentException("The value cannot be empty", nameof(s));

            var splittedIdentity = s.Split('@');

            return new Identity
            {
                Name = !string.IsNullOrWhiteSpace(splittedIdentity[0]) ? splittedIdentity[0] : null,
                Domain = splittedIdentity.Length > 1 ? splittedIdentity[1].Split('/')[0] : null
            };
        }

        /// <summary>
        /// Tries to parse the string to a valid Identity.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
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
