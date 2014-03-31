using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol
{
    /// <summary>
    /// MIME media type representation
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class MediaType
    {
        #region Constructor

        public MediaType()
        {

        }

        public MediaType(string mediaType)
        {
            if (string.IsNullOrWhiteSpace(mediaType))
            {
                throw new ArgumentNullException("value");
            }

            var splittedMediaType = mediaType.Split('/');

            if (splittedMediaType.Length != 2)
            {
                throw new FormatException("Invalid media type format");
            }

            this.Type = splittedMediaType[0];

            var splittedSubtype = splittedMediaType[1].Split('+');

            this.Subtype = splittedSubtype[0];

            if (splittedSubtype.Length > 1)
            {
                this.Suffix = splittedSubtype[1];
            }
        }

        public MediaType(string type, string subtype, string suffix)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                throw new ArgumentNullException("type");
            }

            this.Type = type;

            if (string.IsNullOrWhiteSpace(subtype))
            {
                throw new ArgumentNullException("subtype");
            }

            this.Subtype = subtype;

            if (string.IsNullOrWhiteSpace(suffix))
            {
                throw new ArgumentNullException("suffix");
            }

            this.Suffix = suffix;
        }

        #endregion

        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "subtype")]
        public string Subtype { get; set; }

        /// <summary>
        /// Media type suffix
        /// <seealso cref="http://trac.tools.ietf.org/html/draft-ietf-appsawg-media-type-regs-14#section-6"/>
        /// </summary>
        [DataMember(Name = "suffix")]
        public string Suffix { get; set; }

        public override string ToString()
        {
            return string.Format("{0}/{1}+{2}", this.Type, this.Subtype, this.Suffix).TrimEnd('+');
        }
        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            return obj.ToString().Equals(this.ToString(), StringComparison.CurrentCultureIgnoreCase);
        }

        public static bool TryParse(string s, out MediaType mediaType)
        {
            try
            {
                mediaType = new MediaType(s);
                return true;
            }
            catch
            {
                mediaType = null;
                return false;                
            }
        }
    }
}
