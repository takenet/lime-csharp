using Lime.Protocol.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Security
{
    public partial class GuestAuthentication
    {
        [Factory]
        public static GuestAuthentication FromJsonObject(JsonObject jsonObject)
        {
            var authentication = new GuestAuthentication();

            return authentication;
        }
    }
}
