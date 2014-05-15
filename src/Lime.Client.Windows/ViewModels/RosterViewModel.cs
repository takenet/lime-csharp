using FirstFloor.ModernUI;
using Lime.Client.Windows.Converters;
using Lime.Client.Windows.Mvvm;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Client;
using Lime.Protocol.Resources;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Threading;
using System.Collections.Specialized;
using GalaSoft.MvvmLight.Command;
using System.Threading;
using Lime.Protocol.Contents;

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
            this.Identity = _clientChannel.LocalNode.ToIdentity();
            
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
            this.Contacts = new ObservableCollectionEx<ContactViewModel>();
            this.Contacts.CollectionChanged += Contacts_CollectionChanged;

            // Commands
            this.LoadedCommand = new AsyncCommand(LoadedAsync);
            this.OpenConversationCommand = new RelayCommand(OpenConversation, CanOpenConversation);
            this.AddContactCommand = new AsyncCommand(AddContactAsync, CanAddContact);
            this.RemoveContactCommand = new AsyncCommand(RemoveContactAsync, CanRemoveContact);
            this.SharePresenceCommand = new AsyncCommand(SharePresenceAsync, () => CanSharePresence);
            this.UnsharePresenceCommand = new AsyncCommand(UnsharePresenceAsync, () => CanUnsharePresence);
            this.ShareAccountInfoCommand = new AsyncCommand(ShareAccountInfoAsync, () => CanShareAccountInfo);
            this.UnshareAccountInfoCommand = new AsyncCommand(UnshareAccountInfoAsync, () => CanUnshareAccountInfo);
            this.AcceptPendingContactCommand = new AsyncCommand(AcceptPendingContactAsync, () => CanAcceptPendingContact);
            this.RejectPendingContactCommand = new AsyncCommand(RejectPendingContactAsync, () => CanRejectPendingContact);
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
                    return Identity.Name.ToString();
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
                        Presence = new Presence()
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
                        Presence = new Presence()
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
                        ContactsView.Filter = new Predicate<object>(o =>
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
                        });
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
                            new Receipt()
                            {
                                Events = new[]
                                {
                                    Event.Accepted,
                                    Event.Received,
                                    Event.Consumed
                                }
                            },
                            _receiveTimeout.ToCancellationToken());

                        // Gets the user account
                        this.Account = await _clientChannel.GetResourceAsync<Account>(
                            _receiveTimeout.ToCancellationToken());

                        // Gets the roster
                        await GetRosterAsync();

                        // Sets the presence
                        this.Presence = new Presence()
                        {
                            Status = PresenceStatus.Available,
                            RoutingRule = RoutingRule.IdentityByDistance
                        };

                        await SetPresenceAsync();

                        _loaded = true;
                    });
            }
        }

        #endregion

        #region OpenConversation

        public RelayCommand OpenConversationCommand { get; private set; }

        private void OpenConversation()
        {
            base.MessengerInstance.Send<OpenWindowMessage>(
                new OpenWindowMessage()
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

        public AsyncCommand AddContactCommand { get; private set; }

        private Task AddContactAsync()
        {
            return ExecuteAsync(async () =>
                {
                    var identity = _searchOrAddContactIdentity;

                    if (identity != null)
                    {
                        await _clientChannel.SetResourceAsync(
                            new Roster()
                            {
                                Contacts = new[]
                                {                    
                                    new Contact()
                                    {
                                        Identity = _searchOrAddContactIdentity
                                    }
                                }
                            },
                            _receiveTimeout.ToCancellationToken());

                        await GetRosterAsync();
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

        public AsyncCommand RemoveContactCommand { get; private set; }

        private Task RemoveContactAsync()
        {
            return ExecuteAsync(async () =>
                {
                    var selectedContact = SelectedContact;

                    if (selectedContact != null &&
                        selectedContact.Contact != null)
                    {
                        await _clientChannel.DeleteResourceAsync(
                            new Roster()
                            {
                                Contacts = new[]
                                {
                                    SelectedContact.Contact
                                }
                            },
                            _receiveTimeout.ToCancellationToken());

                        await GetRosterAsync();
                    }
                });
        }

        private bool CanRemoveContact()
        {
            return !IsBusy && SelectedContact != null;
        }

        #endregion

        #region SharePresence

        public AsyncCommand SharePresenceCommand { get; private set; }

        public Task SharePresenceAsync()
        {
            return ExecuteAsync(async () =>
                {
                    var selectedContact = SelectedContact;
                    if (selectedContact != null &&
                        selectedContact.Contact != null)
                    {
                        var contact = new Contact()
                        {
                            Identity = selectedContact.Contact.Identity,
                            SharePresence = true
                        };

                        await _clientChannel.SetResourceAsync(
                            new Roster()
                            {
                                Contacts = new[]
                                {
                                    contact
                                }
                            },
                            _receiveTimeout.ToCancellationToken());

                        await GetRosterAsync();
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
                        var contact = new Contact()
                        {
                            Identity = selectedContact.Contact.Identity,
                            SharePresence = false
                        };

                        await _clientChannel.SetResourceAsync(
                            new Roster()
                            {
                                Contacts = new[]
                                {
                                    contact
                                }
                            },
                            _receiveTimeout.ToCancellationToken());

                        await GetRosterAsync();
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
                    var contact = new Contact()
                    {
                        Identity = selectedContact.Contact.Identity,
                        ShareAccountInfo = true
                    };

                    await _clientChannel.SetResourceAsync(
                        new Roster()
                        {
                            Contacts = new[] { contact }
                        },
                        _receiveTimeout.ToCancellationToken());

                    await GetRosterAsync();
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
                    var contact = new Contact()
                    {
                        Identity = selectedContact.Contact.Identity,
                        ShareAccountInfo = false
                    };

                    await _clientChannel.SetResourceAsync(
                        new Roster()
                        {
                            Contacts = new[]
                                {
                                    contact
                                }
                        },
                        _receiveTimeout.ToCancellationToken());

                    await GetRosterAsync();
                }
            });
        }

        public bool CanUnshareAccountInfo
        {
            get { return !IsBusy && SelectedContact != null && SelectedContact.ShareAccountInfo; }
        }

        #endregion

        #region AcceptPendingContact

        public AsyncCommand AcceptPendingContactCommand { get; private set; }

        public Task AcceptPendingContactAsync()
        {
            return ExecuteAsync(async () =>
            {
                var selectedContact = SelectedContact;
                if (selectedContact != null &&
                    selectedContact.Contact != null)
                {
                    var contact = new Contact()
                    {
                        Identity = selectedContact.Contact.Identity,
                        IsPending = false,
                        ShareAccountInfo = true,
                        SharePresence = true
                    };

                    await _clientChannel.SetResourceAsync(
                        new Roster()
                        {
                            Contacts = new[]
                                {
                                    contact
                                }
                        },
                        _receiveTimeout.ToCancellationToken());

                    await GetRosterAsync();
                }
            });
        }

        public bool CanAcceptPendingContact
        {
            get { return !IsBusy && SelectedContact != null && SelectedContact.IsPending; }
        }

        #endregion

        #region RejectPendingContact

        public AsyncCommand RejectPendingContactCommand { get; private set; }

        public Task RejectPendingContactAsync()
        {
            return ExecuteAsync(async () =>
            {
                var selectedContact = SelectedContact;
                if (selectedContact != null &&
                    selectedContact.Contact != null)
                {
                    var contact = new Contact()
                    {
                        Identity = selectedContact.Contact.Identity
                    };

                    await _clientChannel.DeleteResourceAsync(
                        new Roster()
                        {
                            Contacts = new[]
                                {
                                    contact
                                }
                        },
                        _receiveTimeout.ToCancellationToken());

                    await GetRosterAsync();
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
                var contactViewModel = this.Contacts.FirstOrDefault(c => c.Contact.Identity.Equals(message.From.ToIdentity()));

                if (contactViewModel == null)
                {
                    // Received a message from someone not in the roster
                    contactViewModel = new ContactViewModel(
                        new Contact()
                        {
                            Identity = message.From.ToIdentity()
                        },
                        _clientChannel);
                    
                    this.Contacts.Add(contactViewModel);                    
                }

                if (contactViewModel != null)
                {
                    contactViewModel.Conversation.ReceiveMessage(message);

                    // Don't focus/show the window if its a chat state message
                    if (!(message.Content is ChatState))
                    {
                        base.MessengerInstance.Send<OpenWindowMessage>(
                            new OpenWindowMessage()
                            {
                                WindowName = "Conversation",
                                DataContext = contactViewModel.Conversation
                            });
                    }
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

        private async Task GetRosterAsync()
        {
            var roster = await _clientChannel.GetResourceAsync<Roster>(
                _receiveTimeout.ToCancellationToken());

            this.Contacts.Clear();

            foreach (var contact in roster.Contacts)
            {
                this.Contacts.Add(new ContactViewModel(contact, _clientChannel));
            }
        }

        private Task SetPresenceAsync()
        {
            return _clientChannel.SetResourceAsync(
                this.Presence,
                _receiveTimeout.ToCancellationToken());
        }

        private async void Contacts_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!ModernUIHelper.IsInDesignMode)
            {
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
                {
                    var addedContacts = e.NewItems.Cast<ContactViewModel>();

                    var cancellationToken = _receiveTimeout.ToCancellationToken();

                    foreach (var contactViewModel in addedContacts)
                    {
                        try
                        {
                            await base.ExecuteAsync(async () =>
                                {
                                    contactViewModel.Presence = await _clientChannel.GetResourceAsync<Presence>(
                                        new Node()
                                        {
                                            Name = contactViewModel.Contact.Identity.Name,
                                            Domain = contactViewModel.Contact.Identity.Domain
                                        },
                                        cancellationToken
                                    );
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
                    await _clientChannel.SendFinishingSessionAsync();
                    var session = await _clientChannel.ReceiveFinishedSessionAsync(_receiveTimeout.ToCancellationToken());
                }

                _clientChannel.DisposeIfDisposable();
            }
        }

        #endregion
    }
}
