using System.Reflection;
using Lime.Protocol.Serialization;

namespace Lime.Messaging
{
    /// <summary>
    /// Allow the registration of messages document types.
    /// </summary>
    public class Registrator
    {
        public static void RegisterDocuments()
        {
            TypeUtil.RegisterDocuments(typeof(Registrator).GetTypeInfo().Assembly);
        }
    }
}
