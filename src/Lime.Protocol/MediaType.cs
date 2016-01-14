using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol
{
    /// <summary>
    /// MIME media type representation.
    /// <a href="http://trac.tools.ietf.org/html/rfc2045" />
    /// </summary>
    public class MediaType
    {
        #region Constructor

        public MediaType()
        {

        }

        public MediaType(string type, string subtype, string suffix = null)
        {
            if (string.IsNullOrWhiteSpace(type)) throw new ArgumentNullException(nameof(type));            
            if (string.IsNullOrWhiteSpace(subtype)) throw new ArgumentNullException(nameof(subtype));
            
            Type = type;
            Subtype = subtype;
            Suffix = suffix;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// The top-level type identifier. The valid values are text, application, image, audio and video.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The media type subtype.
        /// </summary>
        public string Subtype { get; set; }

        /// <summary>
        /// Media type suffix        
        /// </summary>
        /// <a href="http://tools.ietf.org/html/rfc6839"/>
        public string Suffix { get; set; }

        /// <summary>
        /// Indicates if the MIME represents a JSON type.
        /// </summary>
        public bool IsJson
        {
            get
            {
                return (Suffix != null && Suffix.Equals(SubTypes.JSON, StringComparison.OrdinalIgnoreCase)) ||
                       (Subtype != null && Subtype.Equals(SubTypes.JSON, StringComparison.OrdinalIgnoreCase));
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <c cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            if (string.IsNullOrWhiteSpace(this.Suffix))
            {
                return string.Format("{0}/{1}", this.Type, this.Subtype);
            }
            else
            {
                return string.Format("{0}/{1}+{2}", this.Type, this.Subtype, this.Suffix);
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
            return this.ToString().GetHashCode();
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
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
                   (this.Suffix == null && mediaType.Suffix == null || (this.Suffix != null && mediaType.Suffix != null && this.Suffix.Equals(mediaType.Suffix, StringComparison.CurrentCultureIgnoreCase)));
        }

        /// <summary> 
        /// Parses the string to a MediaType object.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">s</exception>
        /// <exception cref="System.FormatException">Invalid media type format</exception>
        public static MediaType Parse(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                throw new ArgumentNullException("s");
            }

            var splittedMediaType = s.Split(';')[0].Split('/');
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

        #endregion

        public static class DiscreteTypes
        {
            public static string Application = "application";

            public static string Text = "text";

            public static string Image = "image";

            public static string Audio = "audio";

            public static string Video = "video";
        }

        public static class CompositeTypes
        {
            public static string Message = "message";

            public static string Multipart = "multipart";
        }

        public static class SubTypes
        {
            public static string Plain = "plain";

            public static string JSON = "json";

            public static string XML = "xml";

            public static string HTML = "html";

            public static string JPeg = "jpeg";

            public static string Bitmap = "bmp";

            public static string Javascript = "javascript";
        }
    }
}
