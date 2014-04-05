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
        /// Writes the json.
        /// </summary>
        /// <param name="writer">The writer.</param>
        public override void WriteJson(IJsonWriter writer)
        {
            writer.WriteArrayProperty(CONTENT_TYPES_KEY, this.ContentTypes, true);
            writer.WriteArrayProperty(RESOURCE_TYPES_KEY, this.ResourceTypes, true);
        }

        [Factory]
        public static Capability FromJsonObject(JsonObject jsonObject)
        {
            var capability = new Capability();
            capability.ContentTypes = jsonObject.GetArrayOrNull<MediaType>(CONTENT_TYPES_KEY, v => MediaType.Parse((string)v));
            capability.ResourceTypes = jsonObject.GetArrayOrNull<MediaType>(RESOURCE_TYPES_KEY, v => MediaType.Parse((string)v));
            return capability;
        }
    }
}
