using Lime.Client.TestConsole.ViewModels;
using System;
using System.Globalization;
using System.Windows.Controls;

namespace Lime.Client.TestConsole.ValidationRules
{
    public class JsonEnvelopeValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            try
            {
                var inputJson = value?.ToString();
                EnvelopeViewModel.Parse(value.ToString());
            }
            catch (Exception exception)
            {
                return new ValidationResult(false, exception.Message);
            }

            return new ValidationResult(true, null);
        }
    }
}
