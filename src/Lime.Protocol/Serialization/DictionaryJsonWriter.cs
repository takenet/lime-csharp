using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Serialization
{
    public class DictionaryJsonWriter : IJsonWriter
    {
        #region Private Fields

        private IDictionary<string, object> _jsonDictionary;
        private bool _writeDefaultValues;

        #endregion

        #region Constructor

        public DictionaryJsonWriter(bool writeDefaultValues = false)
        {
            _writeDefaultValues = writeDefaultValues;
            _jsonDictionary = new Dictionary<string, object>();
        }

        #endregion

        #region IJsonWriter Members

        public void WriteBoolProperty(string propertyName, bool value)
        {
            if (_writeDefaultValues || 
                value != false)
            {
                _jsonDictionary.Add(propertyName, value);
            }            
        }

        public void WriteDateTimeProperty(string propertyName, DateTime value)
        {
            if (_writeDefaultValues ||
                value != DateTime.MinValue)
            {
                _jsonDictionary.Add(propertyName, value);
            }
        }

        public void WriteDictionaryProperty(string propertyName, IDictionary<string, string> dictionary)
        {
            if (_writeDefaultValues || dictionary != null)
            {
                _jsonDictionary.Add(propertyName, dictionary);
            }
        }

        public void WriteGuidProperty(string propertyName, Guid value)
        {
            if (_writeDefaultValues || value != Guid.Empty)
            {
                _jsonDictionary.Add(propertyName, value);
            }
        }

        public void WriteIntProperty(string propertyName, int value)
        {
            if (_writeDefaultValues || value != 0)
            {
                _jsonDictionary.Add(propertyName, value);
            }
        }

        public void WriteArrayProperty(string propertyName, IEnumerable items)
        {
            if (items != null)
            {
                List<object> itemList = new List<object>();

                foreach (var item in items)
                {
                    if (item is IJsonWritable)
                    {
                        var jsonWritable = (IJsonWritable)item;
                        var writer = new DictionaryJsonWriter();
                        jsonWritable.WriteJson(writer);
                        itemList.Add(writer.ToDictionary());
                    }
                    else if (item is Enum)
                    {
                        itemList.Add(item.ToString().ToCamelCase());
                    }
                    else
                    {
                        itemList.Add(item);
                    }
                }

                _jsonDictionary.Add(propertyName, itemList);
            }
            else if (_writeDefaultValues)
            {
                _jsonDictionary.Add(propertyName, items);
            }
        }

        public void WriteJsonArrayProperty(string propertyName, IEnumerable<IJsonWritable> jsonItems)
        {
            List<IDictionary<string, object>> dictionaryList = new List<IDictionary<string, object>>();

            foreach (var jsonItem in jsonItems)
            {
                var writer = new DictionaryJsonWriter();
                jsonItem.WriteJson(writer);

                dictionaryList.Add(writer.ToDictionary());                
            }

            _jsonDictionary.Add(propertyName, dictionaryList);
        }

        public void WriteJsonProperty(string propertyName, IJsonWritable json)
        {
            var writer = new DictionaryJsonWriter();
            json.WriteJson(writer);
            _jsonDictionary.Add(propertyName, writer.ToDictionary());
        }

        public void WriteLongProperty(string propertyName, long value)
        {
            if (_writeDefaultValues || value != 0)
            {
                _jsonDictionary.Add(propertyName, value);
            }
        }

        public void WriteProperty(string propertyName, object value)
        {
            if (value == null)                
            {
                if (_writeDefaultValues)
                {
                    _jsonDictionary.Add(propertyName, value);
                }
            }
            else if (value is Enum)
            {
                _jsonDictionary.Add(propertyName, value.ToString().ToCamelCase());
            }
            else if (value is IJsonWritable)
            {
                WriteJsonProperty(propertyName, (IJsonWritable)value);
            }
            else if (value is IEnumerable<IJsonWritable>)
            {
                WriteJsonArrayProperty(propertyName, (IEnumerable<IJsonWritable>)value);
            }
            else if (value is int || 
                     value is int?)
            {
                WriteIntProperty(propertyName, (int)value);
            }
            else if (value is long ||
                     value is long?)
            {
                WriteLongProperty(propertyName, (long)value);
            }
            else if (value is bool ||
                     value is bool?)
            {
                WriteBoolProperty(propertyName, (bool)value);
            }
            else if (value is DateTime ||
                     value is DateTime?)
            {
                WriteDateTimeProperty(propertyName, (DateTime)value);
            }
            else if (value is Guid ||
                     value is Guid?)
            {
                WriteGuidProperty(propertyName, (Guid)value);
            }
            else
            {
                _jsonDictionary.Add(propertyName, value.ToString());
            }
        }

        public void WriteStringProperty(string propertyName, string value)
        {
            if (value != null || _writeDefaultValues)
            {
                _jsonDictionary.Add(propertyName, value);
            }            
        }

        #endregion


        public IDictionary<string, object> ToDictionary()
        {
            return _jsonDictionary;
        }
    }
}
