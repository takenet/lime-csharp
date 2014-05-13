using FirstFloor.ModernUI.Windows.Navigation;
using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Lime.Client.Windows.ViewModels
{
    public class PageViewModelBase : ViewModelBase
    {
        private SemaphoreSlim _executeSemaphore;
        private DispatcherTimer _errorMessageTimer;

        #region Constructor

        public PageViewModelBase(Uri pageUri)
        {
            PageUri = pageUri;
            _executeSemaphore = new SemaphoreSlim(1);
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

                if (!string.IsNullOrWhiteSpace(_errorMessage))
                {
                    _errorMessageTimer = new DispatcherTimer(
                        TimeSpan.FromSeconds(5),
                        DispatcherPriority.DataBind,
                        (sender, e) => ErrorMessage = null,
                        Dispatcher.CurrentDispatcher);
                }
                else if (_errorMessageTimer != null)
                {
                    _errorMessageTimer.Stop();
                    _errorMessageTimer = null;
                }
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

        

        /// <summary>
        /// Executes the specified func,
        /// synchronizing the access and 
        /// setting the window as busy.
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        protected async Task ExecuteAsync(Func<Task> func)
        {
            if (func == null)
            {
                throw new ArgumentNullException("func");
            }

            await _executeSemaphore.WaitAsync();
            
            try
            {
                IsBusy = true;
                await func();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                _executeSemaphore.Release();
                IsBusy = false;
            }
        }
    }
}
