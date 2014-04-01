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


        /// <summary> 
        /// Parses the string to a MediaType object.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">value</exception>
        /// <exception cref="System.FormatException">Invalid media type format</exception>
        public static MediaType Parse(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                throw new ArgumentNullException("value");
            }

            var splittedMediaType = s.Split('/');

            if (splittedMediaType.Length != 2)
            {
                throw new FormatException("Invalid media type format");
            }
           
            var type = splittedMediaType[0];

            var splittedSubtype = splittedMediaType[1].Split('+');

            var subtype = splittedSubtype[0];

            string suffix = null;

            if (splittedSubtype.Length > 1)
            {
                suffix = splittedSubtype[1];
            }

            return new MediaType(type, subtype, suffix);
        }


        /// <summary>
        /// Try parses the string to a MediaType object.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <param name="mediaType">Type of the media.</param>
        /// <returns></returns>
        public static bool TryParse(string s, out MediaType mediaType)
        {
            try
            {
                mediaType = MediaType.Parse(s);
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
