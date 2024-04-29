using CommunityToolkit.Mvvm.ComponentModel;

namespace Lime.Client.TestConsole.ViewModels
{
    public class VariableViewModel : ObservableRecipient
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


        private string _value;

        public string Value
        {
            get { return _value; }
            set 
            { 
                _value = value;
                OnPropertyChanged(nameof(Value));
            }
        }
    }
}
