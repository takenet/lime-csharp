using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Client.Windows
{
    internal class OpenWindowMessage
    {
        private string _windowName;
        public string WindowName
        {
            get { return _windowName; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                _windowName = value;
            }
        }

        public object DataContext { get; set; }
    }
}
