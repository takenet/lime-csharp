using Lime.Protocol.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Resources
{
    public partial class Account
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

            writer.WriteStringProperty(FULL_NAME_KEY, this.FullName);
            writer.WriteStringProperty(ADDRESS_KEY, this.Address);
            writer.WriteStringProperty(CITY_KEY, this.City);
            writer.WriteStringProperty(EMAIL_KEY, this.Email);
            writer.WriteStringProperty(PHONE_NUMBER_KEY, this.PhoneNumber);
            writer.WriteStringProperty(CELL_PHONE_NUMBER_KEY, this.CellPhoneNumber);
            writer.WriteProperty(IS_TEMPORARY_KEY, this.IsTemporary);
            writer.WriteStringProperty(PASSWORD_KEY, this.Password);
            writer.WriteStringProperty(OLD_PASSWORD_KEY, this.OldPassword);
            writer.WriteProperty(INBOX_SIZE_KEY, this.InboxSize);
            writer.WriteProperty(ALLOW_ANONYMOUS_SENDER_KEY, this.AllowAnonymousSender);
            writer.WriteProperty(ALLOW_UNKNOWN_SENDER_KEY, this.AllowUnknownSender);
            writer.WriteProperty(STORE_MESSAGE_CONTENT_KEY, this.StoreMessageContent);
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

            var document = new Account();
            document.FullName = jsonObject.GetValueOrDefault<string>(FULL_NAME_KEY);
            document.Address = jsonObject.GetValueOrDefault<string>(ADDRESS_KEY);
            document.City = jsonObject.GetValueOrDefault<string>(CITY_KEY);
            document.Email = jsonObject.GetValueOrDefault<string>(EMAIL_KEY);
            document.CellPhoneNumber = jsonObject.GetValueOrDefault<string>(CELL_PHONE_NUMBER_KEY);
            document.IsTemporary = jsonObject.GetValueOrNull<bool>(IS_TEMPORARY_KEY);
            document.Password = jsonObject.GetValueOrDefault<string>(PASSWORD_KEY);
            document.OldPassword = jsonObject.GetValueOrDefault<string>(OLD_PASSWORD_KEY);
            document.InboxSize = jsonObject.GetValueOrNull<int>(INBOX_SIZE_KEY);
            document.AllowAnonymousSender = jsonObject.GetValueOrNull<bool>(ALLOW_ANONYMOUS_SENDER_KEY);
            document.AllowAnonymousSender = jsonObject.GetValueOrNull<bool>(ALLOW_UNKNOWN_SENDER_KEY);
            document.StoreMessageContent = jsonObject.GetValueOrNull<bool>(STORE_MESSAGE_CONTENT_KEY);
           
            return document;
        }
    }
}
