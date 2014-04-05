using Lime.Protocol.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol
{
    public partial class Envelope : IJsonWritable, IJsonSerializable
    {
        #region IJsonWritable Members

        /// <summary>
        /// Writes the JSON representation
        /// of the object to the writer
        /// </summary>
        /// <param name="writer"></param>
        public virtual void WriteJson(IJsonWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            writer.WriteProperty(ID_KEY, this.Id);
            writer.WriteProperty(FROM_KEY, this.From);
            writer.WriteProperty(PP_KEY, this.Pp);
            writer.WriteProperty(TO_KEY, this.To);
            writer.WriteDictionaryProperty(METADATA_KEY, this.Metadata);
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
            using (var writer = new JsonWriter())
            {
                ((IJsonWritable)this).WriteJson(writer);
                return writer.ToString();
            }
        }

        #endregion

        protected static void Populate(Envelope envelope, JsonObject jsonObject)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException("envelope");
            }

            if (jsonObject == null)
            {
                throw new ArgumentNullException("dictionary");
            }

            envelope.Id = jsonObject.GetValueOrDefault<Guid?>(Envelope.ID_KEY, v => new Guid((string)v));
            envelope.From = jsonObject.GetValueOrDefault(Envelope.FROM_KEY, v => Node.Parse((string)v));
            envelope.Pp = jsonObject.GetValueOrDefault(Envelope.PP_KEY, v => Node.Parse((string)v));
            envelope.To = jsonObject.GetValueOrDefault(Envelope.TO_KEY, v => Node.Parse((string)v));
            envelope.Metadata = jsonObject.GetValueOrDefault<IDictionary<string, string>>(Envelope.METADATA_KEY, v => ((IDictionary<string, object>)v).ToDictionary(e => e.Key, e => (string)e.Value));
        }
    }
}
