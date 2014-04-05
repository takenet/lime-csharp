using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Serialization
{
    public interface IJsonWriter
    {
        void WriteBoolProperty(string propertyName, bool value);

        void WriteDateTimeProperty(string propertyName, DateTime value);

        void WriteDictionaryProperty(string propertyName, IDictionary<string, string> dictionary);

        void WriteGuidProperty(string propertyName, Guid value);

        void WriteIntProperty(string propertyName, int value);

        void WriteArrayProperty(string propertyName, IEnumerable items, bool stringToCamelCase = false);

        void WriteJsonArrayProperty(string propertyName, IEnumerable<IJsonSerializable> jsonItems);

        void WriteJsonProperty(string propertyName, IJsonSerializable json);

        void WriteLongProperty(string propertyName, long value);

        void WriteProperty(string propertyName, object value, bool stringToCamelCase = false);

        void WriteStringProperty(string propertyName, string value);

        void WriteValueProperty(string propertyName, string value);
    }
}