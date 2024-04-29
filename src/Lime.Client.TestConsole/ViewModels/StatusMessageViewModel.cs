using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace Lime.Client.TestConsole.ViewModels
{
    public class StatusMessageViewModel : ObservableRecipient
    {        
        private DateTimeOffset _timestamp;

        public DateTimeOffset Timestamp
        {
            get { return _timestamp; }
            set 
            { 
                _timestamp = value;
                OnPropertyChanged(nameof(Timestamp));
                OnPropertyChanged(nameof(TimestampFormat));
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
                OnPropertyChanged(nameof(IsError));
            }
        }

        private string _message;

        public string Message
        {
            get { return _message; }
            set 
            { 
                _message = value;
                OnPropertyChanged(nameof(Message));
            }
        }
    }
}