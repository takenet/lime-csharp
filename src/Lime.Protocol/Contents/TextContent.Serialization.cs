using Lime.Protocol.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Contents
{
    public partial class TextContent
    {
        /// <summary>
        /// Writes the json.
        /// </summary>
        /// <param name="writer">The writer.</param>
        public override void WriteJson(IJsonWriter writer)
        {
            writer.WriteStringProperty(TEXT_KEY, this.Text);
        }

        [Factory]
        public static TextContent FromJsonObject(JsonObject jsonObject)
        {
            var textContent = new TextContent();
            textContent.Text = jsonObject.GetValueOrDefault(TEXT_KEY, v => (string)v);
            return textContent;
        }
    }
}
