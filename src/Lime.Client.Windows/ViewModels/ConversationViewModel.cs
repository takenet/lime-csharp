using GalaSoft.MvvmLight;
using Lime.Client.Windows.Shared;
using Lime.Protocol;
using Lime.Protocol.Contents;
using Lime.Protocol.Resources;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Client.Windows.ViewModels
{
    public class ConversationViewModel : ViewModelBase
    {
        private ContactViewModel _contactViewModel;

        #region Constructor

        public ConversationViewModel(ContactViewModel contactViewModel)
            : this()
        {

        }
        public ConversationViewModel()
        {
            Messages = new ObservableCollection<MessageViewModel>();
        }

        #endregion

        #region Public properties

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

        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                RaisePropertyChanged(() => Name);
                Title = string.Format("{0} - Opa Messenger", Name);
            }
        }

        private PresenceStatus _presenceStatus;
        public PresenceStatus PresenceStatus
        {
            get { return _presenceStatus; }
            set
            {
                _presenceStatus = value;
                RaisePropertyChanged(() => PresenceStatus);
            }
        }

        private ChatStateEvent _chatState;
        /// <summary>
        /// Remote chat state
        /// </summary>
        public ChatStateEvent ChatState
        {
            get { return _chatState; }
            set
            {
                _chatState = value;
                RaisePropertyChanged(() => ChatState);
            }
        }

        private long _lastLocalChatStateChangeTicks;

        private ChatStateEvent _localChatState;
        public ChatStateEvent LocalChatState
        {
            get { return _localChatState; }
            set
            {
                _lastLocalChatStateChangeTicks = DateTime.UtcNow.Ticks;

                if (_localChatState != value)
                {
                    _localChatState = value;

                    LocalChatStateChanged.RaiseEvent(this, new ChatStateEventArgs(value));
                }
            }
        }

        private string _messageText;
        public string MessageText
        {
            get { return _messageText; }
            set
            {
                _messageText = value;
                RaisePropertyChanged(() => MessageText);
                //SendMessageCommand.RaiseCanExecuteChanged();
            }
        }

        private bool _hasUnreadMessage;
        public bool HasUnreadMessage
        {
            get { return _hasUnreadMessage; }
            set
            {
                _hasUnreadMessage = value;
                RaisePropertyChanged(() => HasUnreadMessage);


                var flashMode = FlashMode.Stop;

                if (value)
                {
                    flashMode = FlashMode.All;
                }

                var flashWindowMessage = new FlashWindowMessage(this, flashMode);
                base.MessengerInstance.Send(flashWindowMessage);
            }
        }

        private bool _isFocused;
        public bool IsFocused
        {
            get { return _isFocused; }
            set
            {
                _isFocused = value;
                RaisePropertyChanged(() => IsFocused);
            }
        }

        public ObservableCollection<MessageViewModel> Messages { get; set; }


        public event EventHandler<ChatStateEventArgs> LocalChatStateChanged;
        #endregion

        

    }

    public class ChatStateEventArgs : EventArgs
    {
        public ChatStateEventArgs(ChatStateEvent chatState)
        {
            this.ChatState = chatState;
        }

        public ChatStateEvent ChatState { get; private set; }
    }
}
