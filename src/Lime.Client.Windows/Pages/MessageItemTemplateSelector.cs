using Lime.Client.Windows.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Lime.Client.Windows.Pages
{
    public class MessageItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate InputMessage { get; set; }

        public DataTemplate OutputMessage { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var message = item as MessageViewModel;

            if (message != null)
            {
                if (message.Direction == MessageDirection.Input)
                {
                    return InputMessage;
                }
                else
                {
                    return OutputMessage;
                }
            }

            return base.SelectTemplate(item, container);
        }
    }
}
