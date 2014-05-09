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
    public class Identity : IIdentity
    {
        /// <summary>
        /// Identity unique name 
        /// on his domain
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Network domain name 
        /// of the identity
        /// </summary>
        public string Domain { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            if (string.IsNullOrWhiteSpace(this.Domain))
            {
                return this.Name;
            }
            else
            {
                return string.Format("{0}@{1}", Name, Domain);
            }            
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return this.ToString().ToLower().GetHashCode();
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
            var identity = obj as Identity;

            if (identity == null)
            {
                return false;
            }

            return ((this.Name == null && identity.Name == null) || (this.Name != null && this.Name.Equals(identity.Name, StringComparison.CurrentCultureIgnoreCase))) &&
                   ((this.Domain == null && identity.Domain == null) || (this.Domain != null && this.Domain.Equals(identity.Domain, StringComparison.CurrentCultureIgnoreCase)));
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
            if (string.IsNullOrWhiteSpace(s))
            {
                throw new ArgumentNullException("s");
            }

            var splittedIdentity = s.Split('@');

            return new Identity()
            {
                Name = !string.IsNullOrWhiteSpace(splittedIdentity[0]) ? splittedIdentity[0] : null,
                Domain = splittedIdentity.Length > 1 ? splittedIdentity[1] : null
            };
        }

        /// <summary>
        /// Tries to parse the string to a valid Identity;
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
