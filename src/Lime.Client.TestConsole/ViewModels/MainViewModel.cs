using GalaSoft.MvvmLight;
using Lime.Client.TestConsole.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Client.TestConsole.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public MainViewModel()
        {
            this.SelectedSession = new SessionViewModel();
        }


        private SessionViewModel _selectedSession;

        public SessionViewModel SelectedSession
        {
            get { return _selectedSession; }
            set 
            { 
                _selectedSession = value;
                RaisePropertyChanged(() => SelectedSession);
            }
        }

        private ObservableCollectionEx<SessionViewModel> _sessions;

        public ObservableCollectionEx<SessionViewModel> Sessions
        {
            get { return _sessions; }
            set 
            { 
                _sessions = value;
                RaisePropertyChanged(() => Sessions);
            }
        }

    }
}
