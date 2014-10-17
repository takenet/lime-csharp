using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Drawing;
using System.IO;
#if MONO
using Android.Graphics;
using Java.Nio;
#else
using System.Drawing.Imaging;
#endif

namespace Lime.Protocol.Contents
{
    /// <summary>
    /// Represents a flat text content
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public partial class BitmapImage : Document
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
#if MONO
                using (var byteBuffer = ByteBuffer.Allocate(this.Image.ByteCount))
                {
                    this.Image.CopyPixelsToBuffer(byteBuffer);
                    return Convert.ToBase64String(byteBuffer.ToArray<byte>());
                }
#else
                using (var stream = new MemoryStream())
                {
                    this.Image.Save(stream, ImageFormat.Bmp);
                    return Convert.ToBase64String(stream.ToArray());
                }
#endif
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
#if MONO
            image = BitmapFactory.DecodeByteArray(imageBytes, 0, imageBytes.Length);
#else
            using (var stream = new MemoryStream(imageBytes))
            {
                image = new Bitmap(stream);
            }
#endif

            return new BitmapImage()
            {
                Image = image
            };
        }

    }
}
