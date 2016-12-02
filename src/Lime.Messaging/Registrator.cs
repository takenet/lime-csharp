using Lime.Protocol.Serialization;
using Lime.Messaging.Contents;
using Lime.Messaging.Resources;
using Lime.Protocol;

namespace Lime.Messaging
{
    /// <summary>
    /// Allow the registration of messages document types.
    /// </summary>
    public class Registrator
    {
        public static void RegisterDocuments()
        {
            TypeUtil.RegisterDocument<ChatState>();
            TypeUtil.RegisterDocument<DocumentSelect>();
            TypeUtil.RegisterDocument<Input>();
            TypeUtil.RegisterDocument<Invoice>();
            TypeUtil.RegisterDocument<InvoiceStatus>();
            TypeUtil.RegisterDocument<Location>();
            TypeUtil.RegisterDocument<MediaLink>();
            TypeUtil.RegisterDocument<PaymentReceipt>();
            TypeUtil.RegisterDocument<PlainText>();
            TypeUtil.RegisterDocument<Select>();
            TypeUtil.RegisterDocument<WebLink>();
            TypeUtil.RegisterDocument<Account>();
            TypeUtil.RegisterDocument<Capability>();
            TypeUtil.RegisterDocument<Contact>();
            TypeUtil.RegisterDocument<Delegation>();
            TypeUtil.RegisterDocument<Group>();
            TypeUtil.RegisterDocument<GroupMember>();
            TypeUtil.RegisterDocument<Ping>();
            TypeUtil.RegisterDocument<Presence>();
            TypeUtil.RegisterDocument<Quota>();
            TypeUtil.RegisterDocument<Receipt>();
        }
    }
}
