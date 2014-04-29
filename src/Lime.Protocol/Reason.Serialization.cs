using Lime.Protocol.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol
{
    public partial class Reason : IJsonSerializable, IJsonWritable
    {
        #region IJsonSerializable Members

        /// <summary>
        /// Serializes the instance value
        /// to a JSON string representation
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public string ToJson()
        {
            using (var writer = new TextJsonWriter())
            {
                this.WriteJson(writer);
                return writer.ToString();
            }
        }

        #endregion

        #region IJsonWritable Members

        /// <summary>
        /// Writes the JSON representation
        /// of the object to the writer
        /// </summary>
        /// <param name="writer"></param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void WriteJson(IJsonWriter writer)
        {
            writer.WriteIntProperty(CODE_KEY, this.Code);
            writer.WriteStringProperty(DESCRIPTION_KEY, this.Description);
        }

        #endregion


        [Factory]
        public static Reason FromJsonObject(JsonObject jsonObject)
        {
            var reason = new Reason();

            reason.Code = jsonObject.GetValueOrDefault<int>(CODE_KEY);
            reason.Description = jsonObject.GetValueOrDefault<string>(DESCRIPTION_KEY);

            return reason;
        }
    }
}
