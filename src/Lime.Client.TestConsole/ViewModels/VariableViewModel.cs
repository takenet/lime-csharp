using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Client.TestConsole.ViewModels
{
    public class VariableViewModel : ViewModelBase
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


        private string _value;

        public string Value
        {
            get { return _value; }
            set 
            { 
                _value = value;
                RaisePropertyChanged(() => Value);
            }
        }
    }
}
