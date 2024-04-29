using System.ComponentModel;
using System.Windows;

namespace Lime.Client.TestConsole.ViewModels
{
    public static class UIHelper
    {
        private static bool? isInDesignMode;

        //
        // Summary:
        //     Determines whether the current code is executed in a design time environment
        //     such as Visual Studio or Blend.
        public static bool IsInDesignMode
        {
            get
            {
                if (!isInDesignMode.HasValue)
                {
                    isInDesignMode = DesignerProperties.GetIsInDesignMode(new DependencyObject());
                }

                return isInDesignMode.Value;
            }
        }
    }
}
