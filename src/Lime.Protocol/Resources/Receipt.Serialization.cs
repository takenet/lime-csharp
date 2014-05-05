using Lime.Protocol.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Resources
{
    public partial class Receipt
    {
        /// <summary>
        /// Writes the json to the
        /// specified writer.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <exception cref="System.ArgumentNullException">writer</exception>
        public override void WriteJson(IJsonWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            writer.WriteArrayProperty(EVENTS_KEY, this.Events);
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

            var document = new Receipt();
            document.Events = jsonObject.GetEnumArrayOrNull<Event>(EVENTS_KEY);
            return document;
        }
    }
}
