using Newtonsoft.Json;
using System;

namespace Lime.Protocol.Serialization.Newtonsoft.Converters
{
    class IdentityJsonConverter : StringBasedTypeJsonConverter<Identity>
    {
        protected override Identity CreateInstance(string tokenValue)
        {
            return Identity.Parse(tokenValue);
        }
    }
}