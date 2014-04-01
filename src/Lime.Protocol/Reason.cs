using Lime.Protocol.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol
{
    /// <summary>
    /// Represents a known reason for
    /// events occurred during the client-server 
    /// interactions.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class Reason : IJsonSerializable, IJsonWritable
    {
        public const string CODE_KEY = "code";
        public const string DESCRIPTION_KEY = "description";

        [DataMember(Name = CODE_KEY)]
        public int Code { get; set; }

        [DataMember(Name = DESCRIPTION_KEY)]
        public string Description { get; set; }

        #region IJsonSerializable Members

        /// <summary>
        /// Serializes the instance value
        /// to a JSON string representation
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public string ToJson()
        {
            using (var writer = new JsonWriter())
            {
                WriteJson(writer);
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


        internal static Reason FromJsonObject(JsonObject jsonObject)
        {
            var reason = new Reason();

            reason.Code = jsonObject.GetValueOrDefault<int>(CODE_KEY);
            reason.Description = jsonObject.GetValueOrDefault<string>(DESCRIPTION_KEY);

            return reason;
        }
    }
}
