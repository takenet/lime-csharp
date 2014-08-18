using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Resources
{
    /// <summary>
    /// Represents a contact information.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public partial class Contact : Document, IIdentity
    {
        public const string MIME_TYPE = "application/vnd.lime.contact+json";

        public const string IDENTITY_KEY = "identity";
        public const string NAME_KEY = "name";
        public const string IS_PENDING_KEY = "isPending";
        public const string SHARE_PRESENCE_KEY = "sharePresence";
        public const string SHARE_ACCOUNT_INFO_KEY = "shareAccountInfo";

        public Contact()
            : base(MediaType.Parse(MIME_TYPE))
        {

        }

        /// <summary>
        /// The identity of the contact, 
        /// in the name@domain format.
        /// </summary>
        [DataMember(Name = IDENTITY_KEY)]
        public Identity Identity { get; set; }

        /// <summary>
        /// The name of the contact. 
        /// This information is only visible by the roster owner.
        /// </summary>
        [DataMember(Name = NAME_KEY)]
        public string Name { get; set; }

        /// <summary>
        /// Determines if the contact is pending for 
        /// acceptance by the roster owner. 
        /// The default value is false.
        /// </summary>
        [DataMember(Name = IS_PENDING_KEY, EmitDefaultValue = false)]
        public bool? IsPending { get; set; }

        /// <summary>
        /// Indicates if the roster owner wants to share 
        /// presence information with the contact. If true, 
        /// the server provides a get delegation permission 
        /// to the contact identity into the roster owner 
        /// presence resource. The default value is true.
        /// </summary>
        [DataMember(Name = SHARE_PRESENCE_KEY, EmitDefaultValue = false)]
        public bool? SharePresence { get; set; }

        /// <summary>
        /// Indicates if the roster owner wants to share account 
        /// information with the contact. If true, the server provides 
        /// a get delegation permission to the contact identity 
        /// into the roster owner account resource. 
        /// The default value is true.
        /// </summary>
        [DataMember(Name = SHARE_ACCOUNT_INFO_KEY, EmitDefaultValue = false)]
        public bool? ShareAccountInfo { get; set; }

        #region IIdentity Members

        [IgnoreDataMember]
        string IIdentity.Name
        {
            get { return Identity != null ? Identity.Name : null; }
        }
        [IgnoreDataMember]
        string IIdentity.Domain
        {
            get { return Identity != null ? Identity.Domain : null; }
        }

        #endregion

        #region Object Members

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return this.Identity != null ? this.Identity.ToString() : string.Empty;
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
            if (obj == null)
            {
                return false;
            }

            return this.ToString().Equals(obj.ToString(), StringComparison.CurrentCultureIgnoreCase);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return this.Identity != null ? this.Identity.GetHashCode() : 0;
        }

        #endregion

    }
}
