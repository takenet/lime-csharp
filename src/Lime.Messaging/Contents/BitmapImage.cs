using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Serialization;
using Lime.Protocol;

namespace Lime.Messaging.Contents
{
    /// <summary>
    /// Represents a flat text content
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class BitmapImage : Document
    {
        public const string MIME_TYPE = "image/bmp";
        public static readonly MediaType MediaType = MediaType.Parse(MIME_TYPE);

        /// <summary>
        /// Initializes a new instance of the <see cref="BitmapImage"/> class.
        /// </summary>
        public BitmapImage()
            : base(MediaType)
        {

        }

        /// <summary>
        /// The bitmap image
        /// </summary>
        public Bitmap Image { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            if (Image == null) return string.Empty;
            using (var stream = new MemoryStream())
            {
                Image.Save(stream, ImageFormat.Bmp);
                return Convert.ToBase64String(stream.ToArray());
            }
        }

        /// <summary>
        /// Parses the string to a 
        /// PlainText instance.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static BitmapImage Parse(string value)
        {
            Bitmap image;

            var imageBytes = Convert.FromBase64String(value);

            using (var stream = new MemoryStream(imageBytes))
            {
                image = new Bitmap(stream);
            }

            return new BitmapImage
            {
                Image = image
            };
        }

    }
}
