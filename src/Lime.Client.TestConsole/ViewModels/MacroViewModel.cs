using System;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using Lime.Client.TestConsole.Macros;

namespace Lime.Client.TestConsole.ViewModels
{
    public class MacroViewModel : ObservableRecipient
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

        private bool _isActive;

        public bool IsActive
        {
            get { return _isActive; }
            set 
            { 
                _isActive = value;
                OnPropertyChanged(nameof(IsActive));
            }
        }

        private Type _type;

        public Type Type
        {
            get { return _type; }
            set 
            { 
                _type = value;
                OnPropertyChanged(nameof(Type));
                
                if (_type != null)
                {                    
                    var macro = Activator.CreateInstance(_type) as IMacro;

                    if (macro != null)
                    {
                        Macro = macro;

                        var macroAttribute = _type.GetCustomAttribute<MacroAttribute>();

                        if (macroAttribute != null)
                        {
                            Name = macroAttribute.Name;
                            Category = macroAttribute.Category;
                            IsActive = macroAttribute.IsActiveByDefault;
                        }
                        else
                        {
                            Name = _type.Name;
                            Category = "Undefined";
                            IsActive = false;
                        }
                    }
                }
            }
        }

        public IMacro Macro { get; private set; }
    }
}
