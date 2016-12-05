using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Client.TestConsole.ViewModels
{
    public class StatusMessageViewModel : ViewModelBase
    {        
        private DateTimeOffset _timestamp;

        public DateTimeOffset Timestamp
        {
            get { return _timestamp; }
            set 
            { 
                _timestamp = value;
                RaisePropertyChanged(() => Timestamp);
                RaisePropertyChanged(() => TimestampFormat);
            }
        }

        public string TimestampFormat
        {
            get { return _timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fff zzz"); }
        }

        private bool _isError;

        public bool IsError
        {
            get { return _isError; }
            set 
            { 
                _isError = value;
                RaisePropertyChanged(() => IsError);
            }
        }

        private string _message;

        public string Message
        {
            get { return _message; }
            set 
            { 
                _message = value;
                RaisePropertyChanged(() => Message);
            }
        }
    }
}