using Lime.Protocol.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Resources
{
    public partial class Capability
    {
        /// <summary>
        /// Writes the json to the
        /// specified writer.
        /// </summary>
        /// <param name="writer">The writer.</param>
        public override void WriteJson(IJsonWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            writer.WriteArrayProperty(CONTENT_TYPES_KEY, this.ContentTypes, true);
            writer.WriteArrayProperty(RESOURCE_TYPES_KEY, this.ResourceTypes, true);
        }

        /// <summary>
        /// Creates an instance of the
        /// type using the passed JsonObject.
        /// </summary>
        /// <param name="jsonObject"></param>
        /// <returns></returns>
        [Factory]
        public static Document FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                throw new ArgumentNullException("jsonObject");
            }

            var document = new Capability();
            document.ContentTypes = jsonObject.GetArrayOrNull<MediaType>(CONTENT_TYPES_KEY, v => MediaType.Parse((string)v));
            document.ResourceTypes = jsonObject.GetArrayOrNull<MediaType>(RESOURCE_TYPES_KEY, v => MediaType.Parse((string)v));
            return document;
        }
    }
}
