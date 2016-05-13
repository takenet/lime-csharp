using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;

namespace Lime.Client.Windows.ViewModels
{
    public class RosterConversationViewModel : PageViewModelBase
    {
        private RosterViewModel _roster;
        private ConversationViewModel _conversation;

        public RosterConversationViewModel(RosterViewModel roster)
            : base(new Uri("/Pages/RosterConversation.xaml", UriKind.Relative))
        {
            if (roster == null) throw new ArgumentNullException(nameof(roster));
            _roster = roster;
            _roster.PropertyChanged += Roster_PropertyChanged;
        }        

        public RosterViewModel Roster
        {
            get { return _roster; }
            set
            {
                _roster = value;
                RaisePropertyChanged();
            }
        }

        public ConversationViewModel Conversation
        {
            get { return _conversation; }
            set
            {
                _conversation = value;
                RaisePropertyChanged();
            }
        }

        private void Roster_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(RosterViewModel.SelectedContact)) && 
                _roster.SelectedContact?.Conversation != null)
            {
                Conversation = _roster.SelectedContact?.Conversation;
            }
        }
    }
}
