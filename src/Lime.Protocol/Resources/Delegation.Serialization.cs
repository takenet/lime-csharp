using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lime.Protocol.Serialization;

namespace Lime.Protocol.Resources
{
    public partial class Delegation
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

            writer.WriteProperty(TARGET_KEY, this.Target);
            writer.WriteArrayProperty(DESTINATIONS_KEY, this.Destinations);
            writer.WriteArrayProperty(COMMANDS_KEY, this.Commands);
            writer.WriteArrayProperty(MESSAGES_KEY, this.Messages);
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

            var document = new Delegation
            {
                Commands = jsonObject.GetArrayOrNull(COMMANDS_KEY,o => DelegationCommand.FromJsonObject((JsonObject)o)),
                Destinations = jsonObject.GetArrayOrNull<Identity>(DESTINATIONS_KEY,o => Identity.Parse((string)o)),
                Target = jsonObject.GetValueOrDefault(TARGET_KEY,o => Node.Parse((string)o)),
                Messages = jsonObject.GetArrayOrNull<MediaType>(DESTINATIONS_KEY, o => MediaType.Parse((string)o))
            };

            return document;
        }
    }

    public partial class DelegationCommand : IJsonWritable
    {
        public void WriteJson(IJsonWriter writer)
        {
            throw new NotImplementedException();
        }
        [Factory]
        public static DelegationCommand FromJsonObject(JsonObject jsonObject)
        {
            var delegationCommand = new DelegationCommand()
            {

            };
            return delegationCommand;
        }
    }
}
