using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Serialization
{
    public class JsonWriter : IJsonWriter, IDisposable
    {
        #region Private fields

        private TextWriter _writer;
        private short _stackedBrackets;
        private bool _commaNeeded;

        #endregion

        #region Constructor

        public JsonWriter()
            : this(new StringWriter())
        {
        }

        public JsonWriter(TextWriter writer)
        {
            _writer = writer;
            WriteOpenBrackets();
        }


        ~JsonWriter()
        {
            Dispose(false);
        }

        #endregion

        #region Private methods

        private void WriteOpenBrackets()
        {
            _writer.Write("{");
            _stackedBrackets++;
            _commaNeeded = false;
        }

        private void WriteCloseBrackets()
        {
            _writer.Write("}");
            _stackedBrackets--;
            _commaNeeded = true;
        }

        private void WriteOpenArray()
        {
            _writer.Write("[");
            _commaNeeded = false;
        }

        private void WriteCloseArray()
        {
            _writer.Write("]");
            _commaNeeded = true;
        }

        private void WriteComma()
        {
            _writer.Write(",");
        }

        private void WriteCollon()
        {
            _writer.Write(":");
        }

        private void WriteQuotation()
        {
            _writer.Write("\"");
        }

        private void WritePropertyName(string propertyName)
        {
            WriteQuotation();
            _writer.Write(propertyName);
            WriteQuotation();
            WriteCollon();
        }

        private void WriteStringValue(string value)
        {
            WriteQuotation();

            for (int i = 0; i < value.Length; i++)
            {
                switch (value[i])
                {
                    case '\n':
                        _writer.Write("\\n");
                        continue;

                    case '\r':
                        _writer.Write("\\r");
                        continue;

                    case '\t':
                        _writer.Write("\\t");
                        continue;

                    case '"':
                    case '\\':
                        _writer.Write('\\');
                        _writer.Write(value[i]);
                        continue;

                    case '\f':
                        _writer.Write("\\f");
                        continue;

                    case '\b':
                        _writer.Write("\\b");
                        continue;
                }

                //Is printable char?
                if (value[i] >= 32 && value[i] <= 126)
                {
                    _writer.Write(value[i]);
                    continue;
                }

                // http://json.org/ spec requires any control char to be escaped
                var hexSeqBuffer = new char[4];

                if (char.IsControl(value[i]))
                {
                    // Default, turn into a \uXXXX sequence
                    IntToHex(value[i], hexSeqBuffer);
                    _writer.Write("\\u");
                    _writer.Write(hexSeqBuffer);
                }
                else
                {
                    _writer.Write(value[i]);
                }
            }

            WriteQuotation();
        }


        public static void IntToHex(int intValue, char[] hex)
        {
            for (var i = 0; i < 4; i++)
            {
                var num = intValue % 16;

                if (num < 10)
                    hex[3 - i] = (char)('0' + num);
                else
                    hex[3 - i] = (char)('A' + (num - 10));

                intValue >>= 4;
            }
        }

        private void WriteValue(string value)
        {
            _writer.Write(value);
        }

        private void WriteJson(IJsonSerializable json)
        {
            _writer.Write(json.ToJson());
        }

        #endregion

        #region IJsonWriter Members

        public void WriteProperty(string propertyName, object value, bool stringToCamelCase = false)
        {
            if (value != null)
            {
                if (value is int || value is int?)
                {
                    WriteIntProperty(propertyName, (int)value);
                }
                else if (value is long || value is long?)
                {
                    WriteLongProperty(propertyName, (long)value);
                }
                else if (value is bool || value is bool?)
                {
                    WriteBoolProperty(propertyName, (bool)value);
                }
                else if (value is DateTime || value is DateTime?)
                {
                    WriteDateTimeProperty(propertyName, (DateTime)value);
                }
                else if (value is DateTimeOffset || value is DateTimeOffset?)
                {
                    WriteDateTimeOffsetProperty(propertyName, (DateTimeOffset)value);
                }
                else if (value is IJsonSerializable)
                {
                    WriteJsonProperty(propertyName, ((IJsonSerializable)value));
                }
                else if (stringToCamelCase)
                {
                    WriteStringProperty(propertyName, value.ToString().ToCamelCase());
                }
                else
                {
                    WriteStringProperty(propertyName, value.ToString());
                }
            }
        }

        public void WriteGuidProperty(string propertyName, Guid value)
        {
            WriteStringProperty(propertyName, value.ToString());
        }

        public void WriteDateTimeProperty(string propertyName, DateTime value)
        {
            WriteStringProperty(propertyName, value.ToString("o", CultureInfo.InvariantCulture));
        }

        public void WriteDateTimeOffsetProperty(string propertyName, DateTimeOffset value)
        {
            WriteStringProperty(propertyName, value.ToString("o", CultureInfo.InvariantCulture));
        }

        public void WriteStringProperty(string propertyName, string value)
        {
            if (value != null)
            {
                if (_commaNeeded)
                {
                    WriteComma();
                }

                WritePropertyName(propertyName);
                WriteStringValue(value);

                _commaNeeded = true;
            }
        }

        public void WriteLongProperty(string propertyName, long value)
        {
            WriteValueProperty(propertyName, value.ToString(CultureInfo.InvariantCulture));
        }

        public void WriteIntProperty(string propertyName, int value)
        {
            WriteValueProperty(propertyName, value.ToString(CultureInfo.InvariantCulture));
        }

        public void WriteBoolProperty(string propertyName, bool value)
        {
            WriteValueProperty(propertyName, value ? "true" : "false");
        }

        public void WriteValueProperty(string propertyName, string value)
        {
            if (_commaNeeded)
            {
                WriteComma();
            }

            WritePropertyName(propertyName);
            WriteValue(value);

            _commaNeeded = true;
        }

        public void WriteJsonProperty(string propertyName, IJsonSerializable json)
        {
            if (json != null)
            {
                if (_commaNeeded)
                {
                    WriteComma();
                }

                WritePropertyName(propertyName);
                WriteJson(json);

                _commaNeeded = true;
            }
        }

        public void WriteDictionaryProperty(string propertyName, IDictionary<string, string> dictionary)
        {
            if (dictionary != null)
            {
                if (_commaNeeded)
                {
                    WriteComma();
                }

                WritePropertyName(propertyName);

                WriteOpenBrackets();

                foreach (var item in dictionary)
                {
                    WriteStringProperty(item.Key, item.Value);
                }

                WriteCloseBrackets();

                _commaNeeded = true;
            }
        }

        public void WriteArrayProperty(string propertyName, IEnumerable items, bool stringToCamelCase = false)
        {
            if (items != null)
            {
                if (_commaNeeded)
                {
                    WriteComma();
                }

                WritePropertyName(propertyName);

                WriteOpenArray();

                foreach (var item in items)
                {
                    if (item != null)
                    {
                        if (_commaNeeded)
                        {
                            WriteComma();
                        }

                        if (item is int ||
                            item is long ||
                            item is bool)
                        {
                            WriteValue(item.ToString());
                        }
                        else if (item is IJsonSerializable)
                        {
                            WriteJson((IJsonSerializable)item);
                        }
                        else if (stringToCamelCase)
                        {
                            WriteStringValue(item.ToString().ToCamelCase());
                        }
                        else
                        {
                            WriteStringValue(item.ToString());
                        }

                        _commaNeeded = true;
                    }
                }

                WriteCloseArray();

                _commaNeeded = true;
            }
        }

        public void WriteJsonArrayProperty(string propertyName, IEnumerable<IJsonSerializable> jsonItems)
        {
            if (jsonItems != null)
            {
                if (_commaNeeded)
                {
                    WriteComma();
                }

                WritePropertyName(propertyName);

                WriteOpenArray();

                foreach (var json in jsonItems)
                {
                    if (json != null)
                    {
                        if (_commaNeeded)
                        {
                            WriteComma();
                        }

                        WriteJson(json);
                        _commaNeeded = true;
                    }
                }

                WriteCloseArray();

                _commaNeeded = true;
            }
        }

        #endregion

        public override string ToString()
        {
            while (_stackedBrackets > 0)
            {
                WriteCloseBrackets();
            }

            return _writer.ToString();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _writer.Dispose();
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}