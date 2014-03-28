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
    /// Allows the nodes to test 
    /// the network connectivity.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class Roster : Document
    {
        public const string MIME_TYPE = "application/vnd.lime.roster+json";

        public Roster()
            : base(new MediaType(MIME_TYPE))
        {

        }

        /// <summary>
        /// Members of the roster
        /// </summary>
        [DataMember(Name = "contacts")]
        public Contact[] Contacts { get; set; }
    }

    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class Contact : IIdentity
    {
        /// <summary>
        /// The identity of the contact, 
        /// in the name@domain format.
        /// </summary>
        [DataMember(Name = "identity")]
        public Identity Identity { get; set; }

        /// <summary>
        /// The name of the contact. 
        /// This information is only visible by the roster owner.
        /// </summary>
        [DataMember(Name = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Determines if the contact is pending for 
        /// acceptance by the roster owner. 
        /// The default value is false.
        /// </summary>
        [DataMember(Name = "isPending", EmitDefaultValue = false)]
        public bool IsPending { get; set; }

        /// <summary>
        /// Indicates if the roster owner wants to share 
        /// presence information with the contact. If true, 
        /// the server provides a get delegation permission 
        /// to the contact identity into the roster owner 
        /// presence resource. The default value is true.
        /// </summary>
        [DataMember(Name = "sharePresence", EmitDefaultValue = false)]
        [DefaultValue(true)]
        public bool SharePresence { get; set; }

        /// <summary>
        /// Indicates if the roster owner wants to share account 
        /// information with the contact. If true, the server provides 
        /// a get delegation permission to the contact identity 
        /// into the roster owner account resource. 
        /// The default value is true.
        /// </summary>
        [DataMember(Name = "shareAccountInfo", EmitDefaultValue = false)]
        [DefaultValue(true)]
        public bool ShareAccountInfo { get; set; }

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

        public override string ToString()
        {
            return Identity != null ? Identity.ToString() : null;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            return this.ToString().Equals(obj.ToString(), StringComparison.CurrentCultureIgnoreCase);
        }

        public override int GetHashCode()
        {
            return this.ToString().ToLower().GetHashCode();
        }
    }
}
