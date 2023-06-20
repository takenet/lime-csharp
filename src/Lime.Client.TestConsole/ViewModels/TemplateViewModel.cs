using CommunityToolkit.Mvvm.ComponentModel;

namespace Lime.Client.TestConsole.ViewModels
{
    public class TemplateViewModel : ObservableRecipient
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

        private string _category;

        public string Category
        {
            get { return _category; }
            set 
            { 
                _category = value;
                OnPropertyChanged(nameof(Category));
            }
        }


        private string _jsonTemplate;
        public string JsonTemplate
        {
            get { return _jsonTemplate; }
            set 
            { 
                _jsonTemplate = value;
                OnPropertyChanged(nameof(JsonTemplate));
            }
        }
    }
}
