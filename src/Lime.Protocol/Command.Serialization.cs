using Lime.Protocol.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol
{
    public partial class Command : IJsonWritable
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
            writer.WriteProperty(TYPE_KEY, this.Type);
            writer.WriteJsonProperty(RESOURCE_KEY, this.Resource);
            writer.WriteProperty(METHOD_KEY, this.Method);
            if (this.Status != CommandStatus.Pending) writer.WriteProperty(STATUS_KEY, this.Status);
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
        public static Command FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                throw new ArgumentNullException("dictionary");
            }

            var command = new Command(null);
            Envelope.Populate(command, jsonObject);
            command.Method = jsonObject.GetEnumValueOrDefault<CommandMethod>(METHOD_KEY);
            command.Status = jsonObject.GetEnumValueOrDefault<CommandStatus>(STATUS_KEY);

            if (jsonObject.ContainsKey(TYPE_KEY) &&
                jsonObject.ContainsKey(RESOURCE_KEY))
            {
                var mediaType = MediaType.Parse((string)jsonObject[TYPE_KEY]);
                var resourceJsonObject = (JsonObject)jsonObject[RESOURCE_KEY];
                command.Resource = Document.FromJsonObject(resourceJsonObject, mediaType);
            }

            if (jsonObject.ContainsKey(REASON_KEY))
            {
                command.Reason = Reason.FromJsonObject((JsonObject)jsonObject[REASON_KEY]);
            }

            return command;
        }
    }
}
