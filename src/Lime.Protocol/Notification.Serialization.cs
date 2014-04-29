using Lime.Protocol.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol
{
    public partial class Notification
    {
        #region IJsonWritable

        /// <summary>
        /// Writes the JSON representation
        /// of the object to the writer
        /// </summary>
        /// <param name="writer"></param>
        public override void WriteJson(IJsonWriter writer)
        {
            base.WriteJson(writer);
            writer.WriteProperty(EVENT_KEY, this.Event);
            writer.WriteJsonProperty(REASON_KEY, this.Reason);
        }

        #endregion

        /// <summary>
        /// Creates an instance of the type
        /// using a dictionary that provides 
        /// the properties values
        /// </summary>
        /// <param name="jsonObject"></param>
        /// <returns></returns>
        [Factory]
        public static Notification FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                throw new ArgumentNullException("dictionary");
            }

            var notification = new Notification();
            Envelope.Populate(notification, jsonObject);
            notification.Event = jsonObject.GetEnumValueOrDefault<Event>(EVENT_KEY);

            if (jsonObject.ContainsKey(REASON_KEY))
            {
                notification.Reason = Reason.FromJsonObject((JsonObject)jsonObject[REASON_KEY]);
            }

            return notification;
        }
    }
}
