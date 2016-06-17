using System;
using System.Runtime.Serialization;
using Lime.Protocol;

namespace Lime.Messaging.Contents
{
    /// <summary>
    /// Defines an item description.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class Item
    {
        public const string DESCRIPTION_KEY = "description";
        public const string IMAGE_URI_KEY = "imageUri";
        public const string IMAGE_TYPE_KEY = "imageType";

        /// <summary>
        /// Gets or sets the product description.
        /// </summary>
        /// <value>
        /// The description.
        /// </value>
        [DataMember(Name = DESCRIPTION_KEY)]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the image URI.
        /// </summary>
        /// <value>
        /// The image URI.
        /// </value>
        [DataMember(Name = IMAGE_URI_KEY)]
        public Uri ImageUri { get; set; }

        /// <summary>
        /// Gets or sets the type of the image.
        /// </summary>
        /// <value>
        /// The type of the image.
        /// </value>
        [DataMember(Name = IMAGE_TYPE_KEY)]
        public MediaType ImageType { get; set; }
    }
}