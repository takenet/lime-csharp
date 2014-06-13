using GalaSoft.MvvmLight;
using Lime.Client.TestConsole.Macros;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace Lime.Client.TestConsole.ViewModels
{
    public class MacroViewModel : ViewModelBase
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

        private bool _isActive;

        public bool IsActive
        {
            get { return _isActive; }
            set 
            { 
                _isActive = value;
                RaisePropertyChanged(() => IsActive);
            }
        }

        private Type _type;

        public Type Type
        {
            get { return _type; }
            set 
            { 
                _type = value;
                RaisePropertyChanged(() => Type);
                
                if (_type != null)
                {                    
                    var macro = Activator.CreateInstance(_type) as IMacro;

                    if (macro != null)
                    {
                        this.Macro = macro;

                        var macroAttribute = _type.GetCustomAttribute<MacroAttribute>();

                        if (macroAttribute != null)
                        {
                            this.Name = macroAttribute.Name;
                            this.Category = macroAttribute.Category;
                            this.IsActive = macroAttribute.IsActiveByDefault;
                        }
                        else
                        {
                            this.Name = _type.Name;
                            this.Category = "Undefined";
                            this.IsActive = false;
                        }
                    }
                }
            }
        }

        public IMacro Macro { get; private set; }
    }
}
