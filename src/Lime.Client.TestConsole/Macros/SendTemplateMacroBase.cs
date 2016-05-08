using System;
using System.Linq;
using System.Threading.Tasks;
using Lime.Client.TestConsole.ViewModels;

namespace Lime.Client.TestConsole.Macros
{
    public abstract class SendTemplateMacroBase : IMacro
    {
        public async Task ProcessAsync(EnvelopeViewModel envelopeViewModel, SessionViewModel sessionViewModel)
        {
            if (envelopeViewModel == null) throw new ArgumentNullException(nameof(envelopeViewModel));
            if (sessionViewModel == null) throw new ArgumentNullException(nameof(sessionViewModel));

            if (ShouldSendTemplate(envelopeViewModel))
            {
                var template = sessionViewModel.Templates.FirstOrDefault(t => t.Name.Equals(TemplateName, StringComparison.OrdinalIgnoreCase));
                if (template != null)
                {
                    sessionViewModel.InputJson = template.JsonTemplate;
                    if (sessionViewModel.SendCommand.CanExecute(null))
                    {
                        await sessionViewModel.SendCommand.ExecuteAsync(null);
                    }
                }
            }            
        }

        protected abstract bool ShouldSendTemplate(EnvelopeViewModel envelopeViewModel);


        protected abstract string TemplateName { get; }
    }
}