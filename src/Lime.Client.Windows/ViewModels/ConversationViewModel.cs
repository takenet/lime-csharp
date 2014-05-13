using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Lime.Client.Windows.Shared;
using Lime.Protocol;
using Lime.Protocol.Contents;
using Lime.Protocol.Resources;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace Lime.Client.Windows.ViewModels
{
    public class ConversationViewModel : ViewModelBase
    {
        #region Private Fields

        private readonly ContactViewModel _contactViewModel;
        private DispatcherTimer _typingTimer;

        #endregion

        #region Constructor

        public ConversationViewModel(ContactViewModel contactViewModel)
            : this()
        {
            _contactViewModel = contactViewModel;
            _contactViewModel.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == "Name")
                {
                    Name = _contactViewModel.Name;
                }
                else if (e.PropertyName == "PresenceStatus")
                {
                    PresenceStatus = _contactViewModel.PresenceStatus;
                }
            };

            this.Name = contactViewModel.Name;
            this.PresenceStatus = contactViewModel.PresenceStatus;

            _typingTimer = new DispatcherTimer(DispatcherPriority.ContextIdle);
            _typingTimer.Interval = TimeSpan.FromMilliseconds(1000);
            _typingTimer.Tick += (sender, e) =>
            {
                if (_lastLocalChatStateChangeTicks > 0 &&
                    LocalChatState != ChatStateEvent.Paused &&
                    TimeSpan.FromTicks(DateTime.UtcNow.Ticks - _lastLocalChatStateChangeTicks) > TimeSpan.FromSeconds(2))
                {
                    LocalChatState = ChatStateEvent.Paused;
                }
            };

            _typingTimer.Start();   
        }
        public ConversationViewModel()
        {
            Messages = new ObservableCollection<MessageViewModel>();
            Messages.CollectionChanged += Messages_CollectionChanged;

            // Commands
            SendMessageCommand = new RelayCommand(SendMessage, CanSendMessage);
            LoadedCommand = new RelayCommand(Loaded);
            ClosingCommand = new RelayCommand(Closing);
            ClosedCommand = new RelayCommand(Closed);
            GotKeyboardFocusCommand = new RelayCommand(GotKeyboardFocus);
            LostKeyboardFocusCommand = new RelayCommand(LostKeyboardFocus);
            PreviewKeyDownCommand = new RelayCommand<KeyEventArgs>(c => PreviewKeyDown(c));

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


        #region Commands

        #region SendMessage

        public RelayCommand SendMessageCommand { get; private set; }

        private void SendMessage()
        {
            var messageViewModel = new MessageViewModel()
            {
                Direction = MessageDirection.Output,
                Text = MessageText
            };

            Messages.Add(messageViewModel);
            MessageText = null;
        }

        private bool CanSendMessage()
        {
            return !string.IsNullOrWhiteSpace(MessageText);
        }

        #endregion

        #region Window Commands

        public RelayCommand LoadedCommand { get; private set; }

        public void Loaded()
        {
            LocalChatState = ChatStateEvent.Starting;
        }

        public RelayCommand ClosingCommand { get; private set; }

        /// <summary>
        /// The window is closing
        /// </summary>
        private void Closing()
        {
            LocalChatState = ChatStateEvent.Gone;
        }

        public RelayCommand ClosedCommand { get; private set; }

        /// <summary>
        /// The window is closed
        /// </summary>
        private void Closed()
        {
        }

        #endregion

        #region Keyboard Commands

        public RelayCommand GotKeyboardFocusCommand { get; private set; }

        private void GotKeyboardFocus()
        {
            IsFocused = true;
            HasUnreadMessage = false;
        }

        public RelayCommand LostKeyboardFocusCommand { get; private set; }

        private void LostKeyboardFocus()
        {
            IsFocused = false;
        }

        public RelayCommand<KeyEventArgs> PreviewKeyDownCommand { get; private set; }

        private void PreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter &&
                SendMessageCommand.CanExecute(null))
            {
                SendMessageCommand.Execute(null);
                LocalChatState = ChatStateEvent.Paused;
            }
            else if (e.Key == Key.Delete || e.Key == Key.Back)
            {
                LocalChatState = ChatStateEvent.Deleting;
            }
            else
            {
                LocalChatState = ChatStateEvent.Composing;
            }
        }

        #endregion

        #endregion

        #region Internal Methods

        internal void ReceiveMessage(Message message)
        {
            if (message.Content is TextContent)
            {
                Messages.Add(new MessageViewModel(message, MessageDirection.Input));
            }
            else if (message.Content is ChatState)
            {
                var chatStateContent = (ChatState)message.Content;
                ChatState = chatStateContent.State;
            }
        }

        internal void ReceiveNotification(Notification notification)
        {
            var message = Messages.FirstOrDefault(m => m.Id == notification.Id);
            if (message != null)
            {
                message.LastEvent = notification.Event;
            }
        }

        #endregion

        #region Private Methods

        private void Messages_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                if (!IsFocused &&
                    e.NewItems.Cast<MessageViewModel>().Any(m => m.Direction == MessageDirection.Input))
                {
                    HasUnreadMessage = true;
                }
            }
        } 

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
