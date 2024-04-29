using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;

namespace Lime.Client.TestConsole.ViewModels
{
    public class ProfileViewModel : ObservableRecipient
    {
        private string _name;

        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        private IDictionary<string, string> _jsonValues;

        public IDictionary<string, string> JsonValues
        {
            get { return _jsonValues; }
            set
            {
                _jsonValues = value;
                OnPropertyChanged(nameof(JsonValues));
            }
        }
    }
}
