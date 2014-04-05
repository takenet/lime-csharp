using Lime.Protocol.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Security
{
    public partial class PlainAuthentication : IJsonWritable
    {
        #region IJsonWritable Members

        public override void WriteJson(IJsonWriter writer)
        {
            writer.WriteStringProperty(PASSWORD_KEY, this.Password);
        }

        #endregion

        [Factory]
        public static PlainAuthentication FromJsonObject(JsonObject jsonObject)
        {
            var authentication = new PlainAuthentication();

            authentication.Password = jsonObject.GetValueOrDefault(PASSWORD_KEY, v => (string)v);

            return authentication;
        }
    }
}
