using System;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using Lime.Client.Windows.Mvvm;

namespace Lime.Client.Windows.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public MainViewModel()
        {
            ContentViewModel = new LoginViewModel();
            TraceViewModel = new TraceViewModel();

            ClosingCommand = new AsyncCommand(p => ClosingAsync());
            ClosedCommand = new AsyncCommand(p => ClosedAsync());
        }

        private PageViewModelBase _contentViewModel;
        public PageViewModelBase ContentViewModel
        {
            get { return _contentViewModel; }
            set
            {
                _contentViewModel = value;
                _contentViewModel.Owner = this;              
                RaisePropertyChanged(() => ContentViewModel);
                RaisePropertyChanged(() => ContentSource);
            }
        }

        public Uri ContentSource => _contentViewModel?.PageUri;

        private TraceViewModel _traceViewModel;
        public TraceViewModel TraceViewModel
        {
            get { return _traceViewModel; }
            set
            {
                _traceViewModel = value;
                RaisePropertyChanged(() => TraceViewModel);
            }
        }

        public AsyncCommand ClosingCommand { get; private set; }

        private async Task ClosingAsync()
        {
            if (ContentViewModel != null)
            {
                await ContentViewModel.OnWindowClosingAsync();
            }
        }

        public AsyncCommand ClosedCommand { get; private set; }

        private async Task ClosedAsync()
        {
            if (ContentViewModel != null)
            {
                await ContentViewModel.OnWindowClosedAsync();
            }
        }
    }
}
