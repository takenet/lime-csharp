using Lime.Protocol.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Resources
{
    public partial class Presence
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

            writer.WriteProperty(STATUS_KEY, this.Status, true);
            writer.WriteStringProperty(MESSAGE_KEY, this.Message);
            writer.WriteProperty(ROUTING_RULE_KEY, this.RoutingRule, true);
            writer.WriteIntProperty(PRIORITY_KEY, this.Priority);
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

            var document = new Presence();
            document.Status = jsonObject.GetEnumValueOrDefault<PresenceStatus>(STATUS_KEY);
            document.Message = jsonObject.GetValueOrDefault<string>(MESSAGE_KEY);
            document.RoutingRule = jsonObject.GetEnumValueOrDefault<RoutingRule>(ROUTING_RULE_KEY);
            document.Priority = jsonObject.GetValueOrDefault<int>(PRIORITY_KEY);

            return document;
        }
    }
}
