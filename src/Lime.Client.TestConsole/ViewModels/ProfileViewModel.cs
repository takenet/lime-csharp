using System;
using System.Collections.Generic;
using System.Reflection;
using GalaSoft.MvvmLight;

namespace Lime.Client.TestConsole.ViewModels
{
    public class ProfileViewModel : ViewModelBase
    {
        private string _name;

        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                RaisePropertyChanged(() => Name);
            }
        }

        private IDictionary<string, string> _jsonValues;

        public IDictionary<string, string> JsonValues
        {
            get { return _jsonValues; }
            set
            {
                _jsonValues = value;
                RaisePropertyChanged(() => JsonValues);
            }
        }
    }
}
