using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
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


        public BitmapImage()
            : base(MediaType.Parse(MIME_TYPE))
        {

        }

        /// <summary>
        /// The bitmap image
        /// </summary>
        public Bitmap Image { get; set; }

        /// <summary>
        /// Gets a Base64 string representation 
        /// of the bitmap
        /// </returns>
        public override string ToString()
        {
            if (this.Image != null)
            {
                using (var stream = new MemoryStream())
                {
                    this.Image.Save(stream, ImageFormat.Bmp);
                    return Convert.ToBase64String(stream.ToArray());
                }
            }

            return null;            
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

            return new BitmapImage()
            {
                Image = image
            };
        }

    }
}
