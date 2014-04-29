using Lime.Protocol.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol
{
    public partial class Document : IJsonWritable, IJsonSerializable
    {
        #region IJsonWritable Members

        /// <summary>
        /// Writes the JSON representation
        /// of the object to the writer
        /// </summary>
        /// <param name="writer"></param>
        public virtual void WriteJson(IJsonWriter writer)
        {
            
        }

        #endregion

        #region IJsonSerializable Members

        /// <summary>
        /// Serializes the instance value
        /// to a JSON string representation
        /// </summary>
        /// <returns></returns>
        public string ToJson()
        {
            using (var writer = new TextJsonWriter())
            {
                WriteJson(writer);
                return writer.ToString();
            }
        }

        #endregion

        public static Document FromJsonObject(JsonObject jsonObject, MediaType mediaType)
        {
            Type documentType;
            if (!TypeUtil.TryGetTypeForMediaType(mediaType, out documentType))
            {
                throw new ArgumentException(string.Format("Could not find a document type for '{0}' media type", mediaType));
            }

            var factoryDelegate = TypeUtil.GetFactoryDelegate(documentType);
            return (Document)factoryDelegate.DynamicInvoke(jsonObject);

        }
    }
}
