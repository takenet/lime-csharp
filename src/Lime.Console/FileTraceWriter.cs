using Lime.Protocol.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Console
{
    public class FileTraceWriter : ITraceWriter
    {
        private string _fileName;


        public FileTraceWriter(string fileName)
        {
            _fileName = fileName;
        }

        #region ITraceWriter Members

        public Task TraceAsync(string data, DataOperation operation)
        {
            File.AppendAllLines(_fileName, new[] { string.Format("{0} - Operation: {1} - Data: {2}", DateTime.UtcNow, operation, data) });
            return Task.FromResult<object>(null);
        }

        public bool IsEnabled
        {
            get { return true; }
        }

        #endregion
    }
}
