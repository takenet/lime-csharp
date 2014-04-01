using Lime.Protocol.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Security
{
    /// <summary>
    /// Defines a guest authentication scheme
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class GuestAuthentication : Authentication
    {
        public GuestAuthentication()
            : base(AuthenticationScheme.Guest)
        {
        }

        public override void WriteJson(IJsonWriter writer)
        {
            
        }

        internal static GuestAuthentication FromJsonObject(JsonObject jsonObject)
        {
            var authentication = new GuestAuthentication();

            return authentication;
        }

    }
}
