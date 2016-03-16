using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using FirstFloor.ModernUI;
using GalaSoft.MvvmLight.Command;
using Lime.Client.Windows.Converters;
using Lime.Client.Windows.Mvvm;
using Lime.Messaging.Contents;
using Lime.Messaging.Resources;
using Lime.Protocol;
using Lime.Protocol.Client;
using Lime.Protocol.Network;

namespace Lime.Client.Windows.ViewModels
{
    public class RosterViewModel : PageViewModelBase
    {
        #region Private Fields

        private readonly LoginViewModel _loginViewModel;
        private readonly IClientChannel _clientChannel;
        private bool _loaded;
        private static TimeSpan _receiveTimeout = TimeSpan.FromSeconds(30);
        private readonly CancellationTokenSource _cancellationTokenSource;
        private Task _listenerTask;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="RosterViewModel"/> class.
        /// </summary>
        /// <param name="clientChannel">The client channel.</param>
        public RosterViewModel(IClientChannel clientChannel, LoginViewModel loginViewModel)
            : this()
        {
            if (clientChannel == null)
            {
                throw new ArgumentNullException("clientChannel");
            }

            if (clientChannel.State != SessionState.Established)
            {
                throw new ArgumentException("The session is in an invalid state");
            }

            _clientChannel = clientChannel;
            Identity = _clientChannel.LocalNode.ToIdentity();
            
            if (loginViewModel == null)
            {
                throw new ArgumentNullException("loginViewModel");
            }

            _loginViewModel = loginViewModel;

            _cancellationTokenSource = new CancellationTokenSource();
            _listenerTask = ListenAsync();

        }

        /// <summary>
        /// Designer constructor
        /// </summary>
        public RosterViewModel()
            : base(new Uri("/Pages/Roster.xaml", UriKind.Relative))
        {
            Contacts = new ObservableCollectionEx<ContactViewModel>();
            Contacts.CollectionChanged += Contacts_CollectionChanged;

            // Commands
            LoadedCommand = new AsyncCommand(LoadedAsync);
            OpenConversationCommand = new RelayCommand(OpenConversation, CanOpenConversation);
            AddContactCommand = new AsyncCommand(AddContactAsync, CanAddContact);
            RemoveContactCommand = new AsyncCommand(RemoveContactAsync, CanRemoveContact);
            SharePresenceCommand = new AsyncCommand(SharePresenceAsync, () => CanSharePresence);
            UnsharePresenceCommand = new AsyncCommand(UnsharePresenceAsync, () => CanUnsharePresence);
            ShareAccountInfoCommand = new AsyncCommand(ShareAccountInfoAsync, () => CanShareAccountInfo);
            UnshareAccountInfoCommand = new AsyncCommand(UnshareAccountInfoAsync, () => CanUnshareAccountInfo);
            AcceptPendingContactCommand = new AsyncCommand(AcceptPendingContactAsync, () => CanAcceptPendingContact);
            RejectPendingContactCommand = new AsyncCommand(RejectPendingContactAsync, () => CanRejectPendingContact);
        }

        #endregion

        #region Public properties

        private Identity _identity;
        
        [TypeConverter(typeof(IdentityTypeConverter))]
        public Identity Identity
        {
            get { return _identity; }
            set
            {
                _identity = value;
                RaisePropertyChanged(() => Identity);
                RaisePropertyChanged(() => Name);
            }
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

                if (Identity != null)
                {
                    return Identity.Name;
                }

                return null;
            }
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

                if (_loaded)
                {
                    IsBusy = true;

                    SetPresenceAsync()
                        .ContinueWith(t =>
                        {
                            if (t.Exception != null)
                            {
                                ErrorMessage = t.Exception.GetBaseException().Message;
                            }

                            IsBusy = false;

                        }, TaskScheduler.FromCurrentSynchronizationContext());
                }
            }
        }

        public PresenceStatus PresenceStatus
        {
            get 
            { 
                if (Presence != null &&
                    Presence.Status.HasValue)
                {
                    return Presence.Status.Value;
                }

                return PresenceStatus.Unavailable; 
            }
            set
            {
                if (Presence == null || 
                    value != Presence.Status)
                {
                    if (Presence == null)
                    {
                        Presence = new Presence
                        {
                            Status = value
                        };
                    }
                    else
                    {
                        Presence.Status = value;
                    }
                }
            }
        }

        public string PresenceMessage
        {
            get
            {
                if (Presence != null)
                {
                    return Presence.Message;
                }

                return null;
            }
            set
            {
                if (Presence == null ||
                    value != Presence.Message)
                {
                    if (Presence == null)
                    {
                        Presence = new Presence
                        {
                            Message = value
                        };
                    }
                    else
                    {
                        Presence.Message = value;
                    }
                }
            }
        }

        public IEnumerable<PresenceStatus> PresenceStatusValues
        {
            get
            {
                return Enum
                    .GetNames(typeof(PresenceStatus))
                    .Select(n => (PresenceStatus)Enum.Parse(typeof(PresenceStatus), n));
            }
        }

        public ICollectionView ContactsView { get; private set; }

        private ObservableCollection<ContactViewModel> _contacts;
        public ObservableCollection<ContactViewModel> Contacts
        {
            get { return _contacts; }
            set
            {
                _contacts = value;

                if (_contacts != null)
                {
                    ContactsView = CollectionViewSource.GetDefaultView(_contacts);
                    ContactsView.GroupDescriptions.Add(new PropertyGroupDescription("PresenceStatus"));
                    ContactsView.SortDescriptions.Add(new SortDescription("SortOrder", ListSortDirection.Ascending));

                    if (!ModernUIHelper.IsInDesignMode)
                    {
                        ContactsView.Filter = o =>
                        {
                            if (!string.IsNullOrWhiteSpace(_searchOrAddContactText))
                            {
                                var c = o as ContactViewModel;

                                if (c != null &&
                                    !string.IsNullOrWhiteSpace(c.Name))
                                {
                                    return CultureInfo.CurrentCulture.CompareInfo.IndexOf(c.Name, _searchOrAddContactText, CompareOptions.IgnoreCase) >= 0;
                                }
                            }

                            return true;
                        };
                    }
                }
            }
        }

        private ContactViewModel _selectedContact;
        public ContactViewModel SelectedContact
        {
            get { return _selectedContact; }
            set
            {
                _selectedContact = value;
                
                RaisePropertyChanged(() => SelectedContact);
                RaisePropertyChanged(() => CanSharePresence);
                RaisePropertyChanged(() => CanUnsharePresence);
                RaisePropertyChanged(() => CanShareAccountInfo);
                RaisePropertyChanged(() => CanUnshareAccountInfo);
                
                RemoveContactCommand.RaiseCanExecuteChanged();
                SharePresenceCommand.RaiseCanExecuteChanged();
                AcceptPendingContactCommand.RaiseCanExecuteChanged();
                RejectPendingContactCommand.RaiseCanExecuteChanged();
            }
        }

        public override bool IsBusy
        {
            get
            {
                return base.IsBusy;
            }
            set
            {
                base.IsBusy = value;

                OpenConversationCommand.RaiseCanExecuteChanged();
                AddContactCommand.RaiseCanExecuteChanged();
                RemoveContactCommand.RaiseCanExecuteChanged();
                SharePresenceCommand.RaiseCanExecuteChanged();
                AcceptPendingContactCommand.RaiseCanExecuteChanged();
                RejectPendingContactCommand.RaiseCanExecuteChanged();
            }
        }

        private Identity _searchOrAddContactIdentity;


        private string _searchOrAddContactText;
        public string SearchOrAddContactText
        {
            get { return _searchOrAddContactText; }
            set
            {
                _searchOrAddContactText = value;
                RaisePropertyChanged(() => SearchOrAddContactText);
                AddContactCommand.RaiseCanExecuteChanged();
                ContactsView.Refresh();
            }
        }

        #endregion

        #region Commands

        #region Loaded

        public AsyncCommand LoadedCommand { get; private set; }

        private async Task LoadedAsync()
        {
            if (!_loaded &&
                !ModernUIHelper.IsInDesignMode &&
                _clientChannel != null)
            {
                await ExecuteAsync(async () =>
                    {
                        // Events for notification
                        await _clientChannel.SetResourceAsync(
                            LimeUri.Parse(UriTemplates.RECEIPT),
                            new Receipt
                            {
                                Events = new[]
                                {
                                    Event.Accepted,
                                    Event.Received,
                                    Event.Consumed
                                }
                            },
                            _receiveTimeout.ToCancellationToken());

                        try
                        {
                            // Gets the user account
                            Account = await _clientChannel.GetResourceAsync<Account>(
                                LimeUri.Parse(UriTemplates.ACCOUNT),
                                _receiveTimeout.ToCancellationToken());
                        }
                        catch (LimeException ex)
                        {
                            if (ex.Reason.Code != ReasonCodes.COMMAND_RESOURCE_NOT_FOUND)
                            {
                                throw;
                            }
                        }

                        // Creates the account if doesn't exists
                        if (Account == null)
                        {
                            Account = new Account
                            {
                                IsTemporary = false,
                                AllowAnonymousSender = false,
                                AllowUnknownSender = false
                            };
                                
                            await _clientChannel.SetResourceAsync(
                                LimeUri.Parse(UriTemplates.ACCOUNT),
                                Account,
                                _receiveTimeout.ToCancellationToken());
                        }                       

                        // Gets the roster
                        await GetContactsAsync();

                        // Sets the presence
                        Presence = new Presence
                        {
                            Status = PresenceStatus.Available,
                            RoutingRule = RoutingRule.Identity
                        };

                        await SetPresenceAsync();

                        _loaded = true;
                    });
            }
        }

        #endregion

        #region OpenConversation

        public RelayCommand OpenConversationCommand { get; }

        private void OpenConversation()
        {
            MessengerInstance.Send(
                new OpenWindowMessage
                {
                    WindowName = "Conversation",
                    DataContext = SelectedContact.Conversation
                });
        }

        private bool CanOpenConversation()
        {
            return !IsBusy && SelectedContact != null;
        }

        #endregion

        #region AddContact

        public AsyncCommand AddContactCommand { get; }

        private Task AddContactAsync()
        {
            return ExecuteAsync(async () =>
                {
                    var identity = _searchOrAddContactIdentity;

                    if (identity != null)
                    {
                        await _clientChannel.SetResourceAsync(
                            LimeUri.Parse(UriTemplates.CONTACTS),
                            new Contact
                            {
                                Identity = _searchOrAddContactIdentity
                            },
                            _receiveTimeout.ToCancellationToken());

                        await GetContactsAsync();
                    }
                });
        }

        private bool CanAddContact()
        {
            return !IsBusy &&
                   Identity.TryParse(SearchOrAddContactText, out _searchOrAddContactIdentity);
        }

        #endregion

        #region RemoveContact

        public AsyncCommand RemoveContactCommand { get; }

        private Task RemoveContactAsync()
        {
            return ExecuteAsync(async () =>
                {
                    var selectedContact = SelectedContact;

                    if (selectedContact != null &&
                        selectedContact.Contact != null)
                    {
                        await _clientChannel.DeleteResourceAsync(
                            LimeUri.Parse(UriTemplates.CONTACT.NamedFormat(new { contactIdentity = SelectedContact.Contact })),
                            _receiveTimeout.ToCancellationToken());

                        await GetContactsAsync();
                    }
                });
        }

        private bool CanRemoveContact()
        {
            return !IsBusy && SelectedContact != null;
        }

        #endregion

        #region SharePresence

        public AsyncCommand SharePresenceCommand { get; }

        public Task SharePresenceAsync()
        {
            return ExecuteAsync(async () =>
                {
                    var selectedContact = SelectedContact;
                    if (selectedContact != null &&
                        selectedContact.Contact != null)
                    {                        
                        await _clientChannel.SetResourceAsync(
                            LimeUri.Parse(UriTemplates.CONTACTS),
                            new Contact
                            {
                                Identity = selectedContact.Contact.Identity,
                                SharePresence = true
                            },
                            _receiveTimeout.ToCancellationToken());

                        await GetContactsAsync();
                    }
                });
        }

        public bool CanSharePresence
        {
            get { return !IsBusy && SelectedContact != null && !SelectedContact.SharePresence; }
        }

        #endregion

        #region UnsharePresence

        public AsyncCommand UnsharePresenceCommand { get; private set; }

        public Task UnsharePresenceAsync()
        {
            return ExecuteAsync(async () =>
                {
                    var selectedContact = SelectedContact;
                    if (selectedContact != null &&
                        selectedContact.Contact != null)
                    {

                        await _clientChannel.SetResourceAsync(
                            LimeUri.Parse(UriTemplates.CONTACTS),
                            new Contact
                            {
                                Identity = selectedContact.Contact.Identity,
                                SharePresence = false
                            },
                            _receiveTimeout.ToCancellationToken());

                        await GetContactsAsync();
                    }
                });
        }

        public bool CanUnsharePresence
        {
            get { return !IsBusy && SelectedContact != null && SelectedContact.SharePresence; }
        }

        #endregion

        #region ShareAccountInfo

        public AsyncCommand ShareAccountInfoCommand { get; private set; }

        public Task ShareAccountInfoAsync()
        {
            return ExecuteAsync(async () =>
            {
                var selectedContact = SelectedContact;
                if (selectedContact != null &&
                    selectedContact.Contact != null)
                {
                    await _clientChannel.SetResourceAsync(
                        LimeUri.Parse(UriTemplates.CONTACTS),
                        new Contact
                        {
                            Identity = selectedContact.Contact.Identity,
                            ShareAccountInfo = true
                        },
                        _receiveTimeout.ToCancellationToken());

                    await GetContactsAsync();
                }
            });
        }

        public bool CanShareAccountInfo
        {
            get { return !IsBusy && SelectedContact != null && !SelectedContact.ShareAccountInfo; }
        }

        #endregion

        #region UnshareAccountInfo

        public AsyncCommand UnshareAccountInfoCommand { get; private set; }

        public Task UnshareAccountInfoAsync()
        {
            return ExecuteAsync(async () =>
            {
                var selectedContact = SelectedContact;
                if (selectedContact != null &&
                    selectedContact.Contact != null)
                {
                    await _clientChannel.SetResourceAsync(
                        LimeUri.Parse(UriTemplates.CONTACTS),
                        new Contact
                        {
                            Identity = selectedContact.Contact.Identity,
                            ShareAccountInfo = false
                        },
                        _receiveTimeout.ToCancellationToken());

                    await GetContactsAsync();
                }
            });
        }

        public bool CanUnshareAccountInfo
        {
            get { return !IsBusy && SelectedContact != null && SelectedContact.ShareAccountInfo; }
        }

        #endregion

        #region AcceptPendingContact

        public AsyncCommand AcceptPendingContactCommand { get; }

        public Task AcceptPendingContactAsync()
        {
            return ExecuteAsync(async () =>
            {
                var selectedContact = SelectedContact;
                if (selectedContact != null &&
                    selectedContact.Contact != null)
                {
                    await _clientChannel.SetResourceAsync(
                        LimeUri.Parse(UriTemplates.CONTACTS),
                        new Contact
                        {
                            Identity = selectedContact.Contact.Identity,
                            IsPending = false,
                            ShareAccountInfo = true,
                            SharePresence = true
                        },
                        _receiveTimeout.ToCancellationToken());

                    await GetContactsAsync();
                }
            });
        }

        public bool CanAcceptPendingContact
        {
            get { return !IsBusy && SelectedContact != null && SelectedContact.IsPending; }
        }

        #endregion

        #region RejectPendingContact

        public AsyncCommand RejectPendingContactCommand { get; }

        public Task RejectPendingContactAsync()
        {
            return ExecuteAsync(async () =>
            {
                var selectedContact = SelectedContact;
                if (selectedContact != null &&
                    selectedContact.Contact != null)
                {
                    await _clientChannel.DeleteResourceAsync(
                        LimeUri.Parse(UriTemplates.CONTACT.NamedFormat(new { contactIdentity =  selectedContact.Contact.Identity })),
                        _receiveTimeout.ToCancellationToken());

                    await GetContactsAsync();
                }
            });
        }

        public bool CanRejectPendingContact
        {
            get { return !IsBusy && SelectedContact != null && SelectedContact.IsPending; }
        }

        #endregion

        #endregion

        #region Private Methods

        private async Task ListenAsync()
        {
            try
            {
                var listenMessagesTask = ListenMessagesAsync();
                var listenNotificationsTask = ListenNotificationsAsync();

                await Task.WhenAll(listenMessagesTask, listenNotificationsTask);
            }
            catch (OperationCanceledException) { }
        }

        private async Task ListenMessagesAsync()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                var message = await _clientChannel.ReceiveMessageAsync(_cancellationTokenSource.Token);
                ProcessMessage(message);
            }
        }

        private void ProcessMessage(Message message)
        {
            if (message.From != null)
            {
                var contactViewModel = Contacts.FirstOrDefault(c => c.Contact.Identity.Equals(message.From.ToIdentity()));

                if (contactViewModel == null)
                {
                    // Received a message from someone not in the roster
                    contactViewModel = new ContactViewModel(
                        new Contact
                        {
                            Identity = message.From.ToIdentity()
                        },
                        _clientChannel);
                    
                    Contacts.Add(contactViewModel);                    
                }
                
                contactViewModel.Conversation.ReceiveMessage(message);

                // Don't focus/show the window if its a chat state message
                if (!(message.Content is ChatState))
                {
                    MessengerInstance.Send(
                        new OpenWindowMessage
                        {
                            WindowName = "Conversation",
                            DataContext = contactViewModel.Conversation
                        });
                }
                
            }
        }

        private async Task ListenNotificationsAsync()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                var notification = await _clientChannel.ReceiveNotificationAsync(_cancellationTokenSource.Token);
                ProcessNotification(notification);
            }
        }

        private void ProcessNotification(Notification notification)
        {
            foreach (var contactViewModel in Contacts)
            {
                if (contactViewModel.Conversation != null)
                {                    
                    contactViewModel.Conversation.ReceiveNotification(notification);                    
                }
            }         
        }

        private async Task GetContactsAsync()
        {
            var contactCollection = await _clientChannel.GetResourceAsync<DocumentCollection>(
                LimeUri.Parse(UriTemplates.CONTACTS),
                _receiveTimeout.ToCancellationToken());

            Contacts.Clear();

            foreach (Contact contact in contactCollection.Items)
            {
                Contacts.Add(new ContactViewModel(contact, _clientChannel));
            }
        }

        private Task SetPresenceAsync()
        {
            return _clientChannel.SetResourceAsync(
                LimeUri.Parse(UriTemplates.PRESENCE),
                Presence,
                _receiveTimeout.ToCancellationToken());
        }

        private async void Contacts_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!ModernUIHelper.IsInDesignMode)
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    var addedContacts = e.NewItems.Cast<ContactViewModel>();

                    var cancellationToken = _receiveTimeout.ToCancellationToken();

                    foreach (var contactViewModel in addedContacts)
                    {
                        try
                        {
                            await ExecuteAsync(async () =>
                                {
                                    var identityPresence = await _clientChannel.GetResourceAsync<Presence>(
                                        LimeUri.Parse(UriTemplates.PRESENCE),
                                        new Node
                                        {
                                            Name = contactViewModel.Contact.Identity.Name,
                                            Domain = contactViewModel.Contact.Identity.Domain
                                        },
                                        cancellationToken);

                                    if (identityPresence.Instances != null &&
                                        identityPresence.Instances.Any())
                                    {
                                        var presence = await _clientChannel.GetResourceAsync<Presence>(
                                            LimeUri.Parse(UriTemplates.PRESENCE),
                                            new Node
                                            {
                                                Name = contactViewModel.Contact.Identity.Name,
                                                Domain = contactViewModel.Contact.Identity.Domain,
                                                Instance = identityPresence.Instances[0]
                                            },
                                            cancellationToken);

                                        if (presence.Instances != null && presence.Instances.Length > 0)
                                        {
                                            var instancePresence = await _clientChannel.GetResourceAsync<Presence>(
                                            LimeUri.Parse(string.Format("{0}/{1}", UriTemplates.PRESENCE, presence.Instances[0])),
                                            new Node
                                            {
                                                Name = contactViewModel.Contact.Identity.Name,
                                                Domain = contactViewModel.Contact.Identity.Domain,
                                                Instance = identityPresence.Instances[0]
                                            },
                                            cancellationToken);
                                            contactViewModel.Presence = instancePresence;
                                        }                                        
                                    }                                    
                                });
                        }
                        catch (LimeException ex)
                        {
                            ErrorMessage = ex.Message;
                        }
                        catch (Exception ex)
                        {
                            ErrorMessage = ex.Message;
                            break;
                        }
                    }
                }
            }
        }

        #endregion

        #region PageViewModelBase Members

        public override async Task OnWindowClosingAsync()
        {
            if (_clientChannel != null)
            {
                if (_clientChannel.State == SessionState.Established)
                {
                    await _clientChannel.SendFinishingSessionAsync(CancellationToken.None);
                    var session = await _clientChannel.ReceiveFinishedSessionAsync(_receiveTimeout.ToCancellationToken());
                }

                _clientChannel.DisposeIfDisposable();
            }
        }

        #endregion
    }
}
