using Lime.Protocol.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Resources
{
    public partial class Roster
    {
        public override void WriteJson(IJsonWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            writer.WriteJsonArrayProperty(CONTACTS_KEY, this.Contacts);
        }       

        [Factory]
        public static Document FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                throw new ArgumentNullException("jsonObject");
            }

            var document = new Roster();
            document.Contacts = jsonObject.GetArrayOrNull(CONTACTS_KEY, i => Contact.FromJsonObject((JsonObject)i));
            return document;
        }
    }

    public partial class Contact : IJsonWritable
    {
        #region IJsonWritable Members

        public void WriteJson(IJsonWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            writer.WriteProperty(IDENTITY_KEY, this.Identity);
            writer.WriteStringProperty(NAME_KEY, this.Name);
            writer.WriteBoolProperty(IS_PENDING_KEY, this.IsPending);            
            writer.WriteProperty(SHARE_PRESENCE_KEY, this.SharePresence);
            writer.WriteProperty(SHARE_ACCOUNT_INFO_KEY, this.ShareAccountInfo);
        }

        #endregion

        [Factory]
        public static Contact FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                throw new ArgumentNullException("jsonObject");
            }

            var contact = new Contact();
            contact.Identity = jsonObject.GetValueOrDefault<Identity>(IDENTITY_KEY, o => Identity.Parse((string)o));
            contact.Name = jsonObject.GetValueOrDefault<string>(NAME_KEY);
            contact.IsPending = jsonObject.GetValueOrDefault<bool>(IS_PENDING_KEY);
            contact.SharePresence = jsonObject.GetValueOrDefault<bool>(SHARE_PRESENCE_KEY);
            contact.ShareAccountInfo = jsonObject.GetValueOrDefault<bool>(SHARE_ACCOUNT_INFO_KEY);
            return contact;
        }
    }
}
