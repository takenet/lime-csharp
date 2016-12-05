using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Lime.Messaging;
using Lime.Client.TestConsole.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Client.TestConsole.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public MainViewModel()
        {
            SelectedSession = new SessionViewModel();

            Title = string.Format(
                "Lime Test Console v{0}",
                Assembly.GetEntryAssembly().GetName().Version);

            ClosingCommand = new RelayCommand(Closing);
            ClosedCommand = new RelayCommand(Closed);
        }


        private string _title;

        public string Title
        {
            get { return _title; }
            set 
            { 
                _title = value;
                RaisePropertyChanged(() => Title);
            }
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

        #region Commands

        public RelayCommand ClosingCommand { get; private set; }

        private void Closing()
        {
            if (SelectedSession != null)
            {
                SelectedSession.SavePreferences();
            }
        }

        public RelayCommand ClosedCommand { get; private set; }

        private void Closed()
        {

        }

        #endregion

    }
}
