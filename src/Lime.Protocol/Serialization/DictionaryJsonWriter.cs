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

        public bool IsEmpty
        {
            get { return _jsonDictionary.Count == 0; }
        }

        public void WriteBooleanProperty(string propertyName, bool value)
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

        public void WriteDictionaryProperty(string propertyName, IDictionary<string, object> dictionary)
        {
            if (_writeDefaultValues || 
                dictionary != null)
            {
                _jsonDictionary.Add(propertyName, dictionary);
            }
        }

        public void WriteIntegerProperty(string propertyName, int value)
        {
            if (_writeDefaultValues || 
                value != 0)
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
                    if (TypeUtil.IsDataContractType(item.GetType()))
                    {                        
                        var writer = new DictionaryJsonWriter();
                        JsonSerializer.Write(item, writer);
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

        public void WriteLongProperty(string propertyName, long value)
        {
            if (_writeDefaultValues || 
                value != 0)
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
                WriteStringProperty(propertyName, value.ToString().ToCamelCase());
            }
            else if (value is string)
            {
                WriteStringProperty(propertyName, (string)value);
            }
            else if (value is IEnumerable)
            {
                WriteArrayProperty(propertyName, (IEnumerable)value);
            }
            else if (value is int || 
                     value is int?)
            {
                WriteIntegerProperty(propertyName, (int)value);
            }
            else if (value is long ||
                     value is long?)
            {
                WriteLongProperty(propertyName, (long)value);
            }
            else if (value is bool ||
                     value is bool?)
            {
                WriteBooleanProperty(propertyName, (bool)value);
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
            else if (TypeUtil.IsDataContractType(value.GetType()))
            {
                WriteJsonProperty(propertyName, value);
            }
            else
            {
                WriteStringProperty(propertyName, value.ToString());                
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

        private void WriteGuidProperty(string propertyName, Guid value)
        {
            if (_writeDefaultValues || value != Guid.Empty)
            {
                _jsonDictionary.Add(propertyName, value);
            }
        }

        private void WriteJsonProperty(string propertyName, object json)
        {
            var writer = new DictionaryJsonWriter();
            JsonSerializer.Write(json, writer);
            _jsonDictionary.Add(propertyName, writer.ToDictionary());
        }

        public IDictionary<string, object> ToDictionary()
        {
            return _jsonDictionary;
        }
    }
}