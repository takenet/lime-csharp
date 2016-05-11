using FirstFloor.ModernUI;
using GalaSoft.MvvmLight;
using Lime.Protocol;
using Lime.Protocol.Client;
using Lime.Messaging.Contents;
using Lime.Messaging.Resources;
using System;
using System.Linq;
using System.Threading;

namespace Lime.Client.Windows.ViewModels
{
    public class ContactViewModel : ViewModelBase
    {
        #region Private Fields

        private readonly IClientChannel _clientChannel;
        private readonly object _syncRoot = new object();

        #endregion

        #region Constructors

        public ContactViewModel(Contact contact, IClientChannel clientChannel)
            : this()
        {
            if (contact == null)
            {
                throw new ArgumentNullException("contact");
            }

            this.Contact = contact;

            _clientChannel = clientChannel;
        }

        /// <summary>
        /// Designer constructor
        /// </summary>
        public ContactViewModel()
        {

        }

        #endregion

        #region Public properties

        private Contact _contact;
        public Contact Contact
        {
            get { return _contact; }
            set
            {
                _contact = value;
                RaisePropertyChanged(() => Contact);
                RaisePropertyChanged(() => Name);
                RaisePropertyChanged(() => IdentityName);
                RaisePropertyChanged(() => IsPending);
                RaisePropertyChanged(() => IsNotPending);
                RaisePropertyChanged(() => SharePresence);
                RaisePropertyChanged(() => ShareAccountInfo);
            }
        }

        public bool IsPending
        {
            get { return _contact.IsPending ?? false; }
        }

        public bool IsNotPending
        {
            get { return !_contact.IsPending ?? true; }
        }

        public bool SharePresence
        {
            get { return _contact.SharePresence ?? false; }
        }

        public bool ShareAccountInfo
        {
            get { return _contact.ShareAccountInfo ?? false; }
        }

        private Account _account;
        public Account Account
        {
            get { return _account; }
            set
            {
                _account = value;
                RaisePropertyChanged(() => Account);
                RaisePropertyChanged(() => Name);
            }
        }

        public string Name
        {
            get
            {
                if (_account != null &&
                    !string.IsNullOrEmpty(_account.FullName))
                {
                    return _account.FullName;
                }

                return _contact.ToString();
            }
        }

        public string IdentityName
        {
            get { return _contact.ToString(); }
        }

        private Presence _presence;
        public Presence Presence
        {
            get { return _presence; }
            set
            {
                _presence = value;
                RaisePropertyChanged(() => Presence);
                RaisePropertyChanged(() => PresenceStatus);
                RaisePropertyChanged(() => PresenceMessage);
                RaisePropertyChanged(() => SortOrder);
            }
        }

        public int SortOrder
        {
            get
            {
                switch (PresenceStatus)
                {
                    case PresenceStatus.Unavailable:
                        return 3;
                    case PresenceStatus.Available:
                        return 0;
                    case PresenceStatus.Busy:
                        return 1;
                    case PresenceStatus.Away:
                        return 2;
                    default:
                        return 3;
                }
            }
        }

        public PresenceStatus PresenceStatus
        {
            get
            {
                if (_presence != null &&
                    _presence.Status.HasValue)
                {
                    return _presence.Status.Value;
                }

                return PresenceStatus.Unavailable;
            }
        }

        public string PresenceMessage
        {
            get
            {
                if (_presence != null)
                {
                    return _presence.Message;
                }
                return null;
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
            }
        }

        private ConversationViewModel _conversation;
        public ConversationViewModel Conversation
        {
            get
            {
                if (_conversation == null)
                {
                    lock (_syncRoot)
                    {
                        if (_conversation == null)
                        {
                            _conversation = new ConversationViewModel(this);
                            _conversation.Messages.CollectionChanged += Conversation_Messages_CollectionChanged;
                            _conversation.LocalChatStateChanged += Conversation_LocalChatStateChanged;

                            _conversation.PropertyChanged += (sender, e) =>
                            {
                                if (e.PropertyName == "HasUnreadMessage")
                                {
                                    HasUnreadMessage = _conversation.HasUnreadMessage;
                                }
                            };
                        }
                    }
                }

                return _conversation;
            }
        }

        #endregion

        #region Private Methods

        private async void Conversation_Messages_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (!ModernUIHelper.IsInDesignMode)
            {
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
                {
                    foreach (var messageViewModel in e.NewItems.Cast<MessageViewModel>().Where(m => m.Direction == MessageDirection.Output))
                    {
                        var message = new Message()
                        {
                            Id = messageViewModel.Id,
                            To = new Node()
                            {
                                Name = this.Contact.Identity.Name,
                                Domain = this.Contact.Identity.Domain
                            },
                            Content = new PlainText()
                            {
                                Text = messageViewModel.Text
                            }
                        };

                        if (_clientChannel.Transport.IsConnected &&
                            _clientChannel.State == SessionState.Established)
                        {
                            await _clientChannel.SendMessageAsync(message, CancellationToken.None);
                        }
                    }
                }
            }
        }

        private async void Conversation_LocalChatStateChanged(object sender, ChatStateEventArgs e)
        {
            if (!ModernUIHelper.IsInDesignMode)
            {
                var message = new Message()
                {
                    To = new Node()
                    {
                        Name = Contact.Identity.Name,
                        Domain = Contact.Identity.Domain                        
                    },
                    Content = new ChatState()
                    {
                        State = e.ChatState
                    }
                };

                if (_clientChannel.Transport.IsConnected &&
                    _clientChannel.State == SessionState.Established)
                {
                    await _clientChannel.SendMessageAsync(message, CancellationToken.None);
                }
            }
        }

        #endregion
    }
}
