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

        public string Type { get; set; }

        public string Subtype { get; set; }

        /// <summary>
        /// Media type suffix
        /// <seealso cref="http://trac.tools.ietf.org/html/draft-ietf-appsawg-media-type-regs-14#section-6"/>
        /// </summary>
        public string Suffix { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("{0}/{1}+{2}", this.Type, this.Subtype, this.Suffix).TrimEnd('+');
        }
        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
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
            var mediaType = obj as MediaType;

            if (mediaType == null)
            {
                return false;
            }

            return this.Type.Equals(mediaType.Type, StringComparison.CurrentCultureIgnoreCase) &&
                   this.Subtype.Equals(mediaType.Subtype, StringComparison.CurrentCultureIgnoreCase) &&
                   this.Suffix.Equals(mediaType.Suffix, StringComparison.CurrentCultureIgnoreCase);
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
