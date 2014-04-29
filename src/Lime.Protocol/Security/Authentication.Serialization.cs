using Lime.Protocol.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Security
{
    public partial class Authentication : IJsonSerializable, IJsonWritable
    {
        #region IJsonWritable Members

        public virtual void WriteJson(IJsonWriter writer)
        {

        }

        #endregion

        #region IJsonSerializable Members

        public string ToJson()
        {
            using (var writer = new TextJsonWriter())
            {
                WriteJson(writer);
                return writer.ToString();
            }
        }

        #endregion

        public static Authentication FromJsonObject(JsonObject jsonObject, AuthenticationScheme scheme)
        {
            if (jsonObject == null)
            {
                throw new ArgumentNullException("authenticationDictionary");
            }

            Type authenticationType;
            if (!TypeUtil.TryGetTypeForAuthenticationScheme(scheme, out authenticationType))
            {
                throw new ArgumentException("Unknown authentication type");
            }

            var factoryDelegate = TypeUtil.GetFactoryDelegate(authenticationType);
            return (Authentication)factoryDelegate.DynamicInvoke(jsonObject);
        }
    }
}
