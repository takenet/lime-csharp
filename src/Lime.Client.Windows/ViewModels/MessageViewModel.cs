using GalaSoft.MvvmLight;
using Lime.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lime.Client.Windows.ViewModels
{
    public class MessageViewModel : ViewModelBase
    {
        private Guid _id;
        public Guid Id
        {
            get { return _id; }
            set
            {
                _id = value;
                RaisePropertyChanged(() => Id);
            }
        }

        private Event? _lastEvent;
        public Event? LastEvent
        {
            get { return _lastEvent; }
            set
            {
                if (_lastEvent == null || (_lastEvent != Event.Failed && value > _lastEvent))
                {
                    _lastEvent = value;
                    RaisePropertyChanged(() => LastEvent);
                }
            }
        }

        private string _text;
        public string Text
        {
            get { return _text; }
            set
            {
                _text = value;
                RaisePropertyChanged(() => Text);
            }
        }

        private DateTime _timestamp;
        public DateTime Timestamp
        {
            get { return _timestamp; }
            set
            {
                _timestamp = value;
                RaisePropertyChanged(() => Timestamp);
            }
        }

        private MessageDirection _direction;
        public MessageDirection Direction
        {
            get { return _direction; }
            set
            {
                _direction = value;
                RaisePropertyChanged(() => Direction);
            }
        }
    }


    public enum MessageDirection
    {
        Input,
        Output
    }
}
