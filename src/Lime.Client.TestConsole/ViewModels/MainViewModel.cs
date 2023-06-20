using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lime.Client.TestConsole.Mvvm;
using System.Reflection;

namespace Lime.Client.TestConsole.ViewModels
{
    public class MainViewModel : ObservableRecipient
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
                OnPropertyChanged(nameof(Title));
            }
        }


        private SessionViewModel _selectedSession;

        public SessionViewModel SelectedSession
        {
            get { return _selectedSession; }
            set 
            { 
                _selectedSession = value;
                OnPropertyChanged(nameof(SelectedSession));
            }
        }

        private ObservableCollectionEx<SessionViewModel> _sessions;

        public ObservableCollectionEx<SessionViewModel> Sessions
        {
            get { return _sessions; }
            set 
            { 
                _sessions = value;
                OnPropertyChanged(nameof(Sessions));
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
