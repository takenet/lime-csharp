using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lime.Protocol.Serialization.Newtonsoft;
using Newtonsoft.Json;

namespace Lime.Protocol.Serialization
{
    public static class JsonDocumentExtensions
    {
        public static string ToJson(this JsonDocument document)
            => JsonConvert.SerializeObject(document, JsonNetSerializer.Settings);
    }
}
