using GalaSoft.MvvmLight;
using Lime.Client.Windows.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Lime.Client.Windows.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        #region Constructor

        public MainViewModel()
        {
            this.ContentViewModel = new LoginViewModel();
            this.TraceViewModel = new TraceViewModel();

            this.ClosingCommand = new AsyncCommand(p => ClosingAsync());
            this.ClosedCommand = new AsyncCommand(p => ClosedAsync());
        }

        #endregion

        #region Public Properties

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

        public Uri ContentSource
        {
            get
            {
                if (_contentViewModel != null)
                {
                    return _contentViewModel.PageUri;
                }

                return null;
            }
        }

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

        #endregion

        #region Commands

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

        #endregion

    }
}
