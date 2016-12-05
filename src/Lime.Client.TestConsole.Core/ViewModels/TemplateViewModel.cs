using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Client.TestConsole.ViewModels
{
    public class TemplateViewModel : ViewModelBase
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

        private string _category;

        public string Category
        {
            get { return _category; }
            set 
            { 
                _category = value;
                RaisePropertyChanged(() => Category);
            }
        }


        private string _jsonTemplate;
        public string JsonTemplate
        {
            get { return _jsonTemplate; }
            set 
            { 
                _jsonTemplate = value;
                RaisePropertyChanged(() => JsonTemplate);
            }
        }
    }
}
