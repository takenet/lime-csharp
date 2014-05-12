using FirstFloor.ModernUI.Windows.Navigation;
using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Client.Windows.ViewModels
{
    public class PageViewModelBase : ViewModelBase
    {
        #region Constructor

        public PageViewModelBase(Uri pageUri)
        {
            PageUri = pageUri;
        }

        #endregion

        #region Public Properties

        private Uri _pageUri;
        public Uri PageUri
        {
            get { return _pageUri; }
            set
            {
                _pageUri = value;
                RaisePropertyChanged(() => PageUri);
            }
        }

        private MainViewModel _owner;
        public MainViewModel Owner
        {
            get { return _owner; }
            set
            {
                _owner = value;
                RaisePropertyChanged(() => Owner);
            }
        }


        private bool _isBusy;
        public virtual bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                _isBusy = value;
                RaisePropertyChanged(() => IsBusy);
                RaisePropertyChanged(() => IsIdle);
            }
        }

        public virtual bool IsIdle
        {
            get { return !_isBusy; }
        }

        private string _errorMessage;
        public virtual string ErrorMessage
        {
            get { return _errorMessage; }
            set
            {
                _errorMessage = value;
                RaisePropertyChanged(() => ErrorMessage);
            }
        } 

        #endregion

        #region Public Methods

        public virtual Task OnActivatedAsync()
        {
            return Task.FromResult<object>(null);
        }

        public virtual Task OnDeactivatedAsync()
        {
            return Task.FromResult<object>(null);
        }

        public virtual Task OnWindowClosingAsync()
        {
            return Task.FromResult<object>(null);
        }

        public virtual Task OnWindowClosedAsync()
        {
            return Task.FromResult<object>(null);
        }

        #endregion
    }
}
