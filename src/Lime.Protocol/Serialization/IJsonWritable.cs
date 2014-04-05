using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Serialization
{
    /// <summary>
    /// Define types that can 
    /// write they value to a JSON
    /// writer
    /// </summary>
    public interface IJsonWritable
    {
        /// <summary>
        /// Writes the JSON representation
        /// of the object to the writer
        /// </summary>
        /// <returns></returns>
        void WriteJson(IJsonWriter writer);
    }
}