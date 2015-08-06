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
    public class TextJsonWriter : IJsonWriter, IDisposable
    {
        #region Private fields

        public const string DATE_FORMAT = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'";

        private TextWriter _writer;
        private short _stackedBrackets;
        private bool _commaNeeded;
        private bool _isEmpty;

        #endregion

        #region Constructor

        public TextJsonWriter()
            : this(new StringWriter())
        {
        }

        public TextJsonWriter(TextWriter writer)
        {
            _isEmpty = true;
            _writer = writer;
            WriteOpenBrackets();
        }

        ~TextJsonWriter()
        {
            Dispose(false);
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Indicates if the writer
        /// is empty (no property has been written)
        /// </summary>
        public bool IsEmpty
        {
            get { return _isEmpty; }
        }

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
            
            if (_isEmpty)
            {
                _isEmpty = false;
            }
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

        private static void IntToHex(int intValue, char[] hex)
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

        private void WriteValueProperty(string propertyName, string value)
        {
            if (_commaNeeded)
            {
                WriteComma();
            }

            WritePropertyName(propertyName);
            WriteValue(value);

            _commaNeeded = true;
        }

        private void WriteValue(string value)
        {
            _writer.Write(value);
        }

        private void WriteJson(object json)
        {
            using (var writer = new TextJsonWriter())
            {
                JsonSerializer.Write(json, writer);
                WriteJsonString(writer.ToString());
            }            
        }

        private void WriteJsonString(string jsonString)
        {
            _writer.Write(jsonString);
        }

        private void WriteJsonProperty(string propertyName, object json)
        {
            if (json != null)
            {
                string jsonString = null;

                using (var writer = new TextJsonWriter())
                {
                    JsonSerializer.Write(json, writer);
                    if (!writer.IsEmpty)
                    {
                        jsonString = writer.ToString();
                    }
                }

                if (!string.IsNullOrWhiteSpace(jsonString))
                {
                    if (_commaNeeded)
                    {
                        WriteComma();
                    }

                    WritePropertyName(propertyName);
                    WriteJsonString(jsonString);

                    _commaNeeded = true;
                }
            }
        }

 

        #endregion

        #region IJsonWriter Members

        public void WriteProperty(string propertyName, object value)
        {
            if (value != null)
            {
                if (value is int || value is int?)
                {
                    WriteIntegerProperty(propertyName, (int)value);
                }
                else if (value is long || value is long?)
                {
                    WriteLongProperty(propertyName, (long)value);
                }
                else if (value is double || value is double?)
                {
                    WriteDoubleProperty(propertyName, (double)value);
                }
                else if (value is bool || value is bool?)
                {
                    WriteBooleanProperty(propertyName, (bool)value);
                }
                else if (value is Enum)
                {
                    WriteStringProperty(propertyName, value.ToString().ToCamelCase());
                }
                else if (TypeUtil.IsDataContractType(value.GetType()))
                {
                    WriteJsonProperty(propertyName, value);
                }
                else if (value is IDictionary<string, object>)
                {
                    WriteDictionaryProperty(propertyName, (IDictionary<string, object>)value);
                }
                else if (value is IDictionary<string, string>)
                {
                    WriteDictionaryProperty(propertyName, ((IDictionary<string, string>)value).ToDictionary(tkey => tkey.Key, tvalue => (object)tvalue.Value));
                }
                else if (value is DateTime || value is DateTime?)
                {
                    WriteDateTimeProperty(propertyName, (DateTime)value);
                }
                else if (value is DateTimeOffset || value is DateTimeOffset?)
                {
                    WriteDateTimeOffsetProperty(propertyName, (DateTimeOffset)value);
                }
                else if (!(value is string) && value is IEnumerable)
                {
                    WriteArrayProperty(propertyName, (IEnumerable)value);
                }
                else
                {
                    WriteStringProperty(propertyName, value.ToString());
                }
            }
        }

        public void WriteDateTimeProperty(string propertyName, DateTime value)
        {
            WriteStringProperty(propertyName, value.ToUniversalTime().ToString(DATE_FORMAT, CultureInfo.InvariantCulture));
        }

        public void WriteDateTimeOffsetProperty(string propertyName, DateTimeOffset value)
        {
            WriteStringProperty(propertyName, value.ToUniversalTime().ToString(DATE_FORMAT, CultureInfo.InvariantCulture));
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

        public void WriteDoubleProperty(string propertyName, double value)
        {
            WriteValueProperty(propertyName, value.ToString(CultureInfo.InvariantCulture));
        }

        public void WriteIntegerProperty(string propertyName, int value)
        {
            WriteValueProperty(propertyName, value.ToString(CultureInfo.InvariantCulture));
        }

        public void WriteBooleanProperty(string propertyName, bool value)
        {
            WriteValueProperty(propertyName, value ? "true" : "false");
        }

        public void WriteDictionaryProperty(string propertyName, IDictionary<string, object> dictionary)
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
                    WriteProperty(item.Key, item.Value);
                }

                WriteCloseBrackets();

                _commaNeeded = true;
            }
        }

        public void WriteArrayProperty(string propertyName, IEnumerable items)
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
                        else if (item is Enum)
                        {
                            WriteStringValue(item.ToString().ToCamelCase());
                        }
                        else if (TypeUtil.IsDataContractType(item.GetType()))
                        {
                            WriteJson(item);
                        }
                        else if (item is IDictionary<string, object>)
                        {
                            WriteOpenBrackets();

                            foreach (var kv in (IDictionary<string, object>)item)
                            {
                                WriteProperty(kv.Key, kv.Value);
                            }

                            WriteCloseBrackets();
                        }
                        else if (item is IDictionary<string, string>)
                        {
                            WriteOpenBrackets();

                            foreach (var kv in (IDictionary<string, string>)item)
                            {
                                WriteProperty(kv.Key, kv.Value);
                            }

                            WriteCloseBrackets();
                        }
                        else if (item is DateTime)
                        {
                            WriteStringValue(((DateTime)item).ToString("o", CultureInfo.InvariantCulture));
                        }
                        else if (item is DateTimeOffset)
                        {
                            WriteStringValue(((DateTimeOffset)item).ToString("o", CultureInfo.InvariantCulture));
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