using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Serialization
{
    /// <summary>
    /// Provides the writing 
    /// of JSON documents.
    /// </summary>
    public interface IJsonWriter
    {
        /// <summary>
        /// Indicates if the writer
        /// is empty (no property has been written)
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// Writes a boolean property
        /// to the document.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="value">The property value.</param>
        void WriteBooleanProperty(string propertyName, bool value);

        /// <summary>
        /// Writes a date time property
        /// to the document.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="value">The value.</param>
        void WriteDateTimeProperty(string propertyName, DateTime value);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="dictionary"></param>
        void WriteDictionaryProperty(string propertyName, IDictionary<string, object> dictionary);

        /// <summary>
        /// Writes a integer property
        /// to the document.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="value">The value.</param>
        void WriteIntegerProperty(string propertyName, int value);

        /// <summary>
        /// Writes an array property
        /// to the document.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="items">The items.</param>
        void WriteArrayProperty(string propertyName, IEnumerable items);

        /// <summary>
        /// Writes a long property
        /// to the document.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="value">The value.</param>
        void WriteLongProperty(string propertyName, long value);

        /// <summary>
        /// Writes a generic property
        /// to the document, using the most
        /// appropriate method accordingly to 
        /// the property type.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="value">The value.</param>
        void WriteProperty(string propertyName, object value);

        /// <summary>
        /// Writes a string property
        /// to the document.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="value">The value.</param>
        void WriteStringProperty(string propertyName, string value);
    }
}