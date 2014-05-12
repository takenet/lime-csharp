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

namespace Lime.Client.Windows.ViewModels
{
    public class RosterViewModel : PageViewModelBase
    {
        #region Private Fields


        private readonly LoginViewModel _loginViewModel;
        private readonly IClientChannel _clientChannel;
        private DispatcherTimer _errorMessageTimer;
        private bool _loaded;
        private static TimeSpan _receiveTimeout = TimeSpan.FromSeconds(30);

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

            if (loginViewModel == null)
            {
                throw new ArgumentNullException("loginViewModel");
            }

            _loginViewModel = loginViewModel;
        }

        /// <summary>
        /// Designer constructor
        /// </summary>
        public RosterViewModel()
            : base(new Uri("/Pages/Roster.xaml", UriKind.Relative))
        {
            LoadedCommand = new AsyncCommand(LoadedAsync);
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

        private PresenceStatus? _previousPresenceStatus;

        private PresenceStatus _presenceStatus;
        public PresenceStatus PresenceStatus
        {
            get { return _presenceStatus; }
            set
            {
                if (value != _presenceStatus)
                {
                    _previousPresenceStatus = _presenceStatus;
                    _presenceStatus = value;
                    RaisePropertyChanged(() => PresenceStatus);

                    //if (_client != null)
                    //{
                    //    IsBusy = true;
                    //    SetPresenceStatusAsync(value, PresenceMessage)
                    //        .ContinueWith(t =>
                    //        {
                    //            App.Current.Dispatcher.Invoke(() =>
                    //            {
                    //                if (t.Exception != null)
                    //                {
                    //                    ErrorMessage = t.Exception.GetBaseException().Message;
                    //                }

                    //                IsBusy = false;
                    //            });
                    //        });
                    //}
                }
            }
        }

        private string _presenceMessage;
        public string PresenceMessage
        {
            get { return _presenceMessage; }
            set
            {
                if (value != _presenceMessage)
                {
                    _presenceMessage = value;
                    RaisePropertyChanged(() => PresenceMessage);

                    //if (_client != null)
                    //{
                    //    IsBusy = true;
                    //    SetPresenceStatusAsync(PresenceStatus, value)
                    //        .ContinueWith(t =>
                    //        {
                    //            App.Current.Dispatcher.Invoke(() =>
                    //            {
                    //                if (t.Exception != null)
                    //                {
                    //                    ErrorMessage = t.Exception.GetBaseException().Message;
                    //                }

                    //                IsBusy = false;
                    //            });
                    //        });
                    //}
                }
            }
        }

        private string _searchOrAddContactText;
        public string SearchOrAddContactText
        {
            get { return _searchOrAddContactText; }
            set
            {
                _searchOrAddContactText = value;
                RaisePropertyChanged(() => SearchOrAddContactText);
                //AddContactCommand.RaiseCanExecuteChanged();
                ContactsView.Refresh();
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

                //RaisePropertyChanged(() => CanSharePresence);
                //RaisePropertyChanged(() => CanUnsharePresence);
                //RaisePropertyChanged(() => CanShareAccountInfo);
                //RaisePropertyChanged(() => CanUnshareAccountInfo);

                //RemoveContactCommand.RaiseCanExecuteChanged();
                //SharePresenceCommand.RaiseCanExecuteChanged();
                //AcceptPendingContactCommand.RaiseCanExecuteChanged();
                //RejectPendingContactCommand.RaiseCanExecuteChanged();
            }
        }      

        #endregion

        #region Commands

        public AsyncCommand LoadedCommand { get; private set; }

        private async Task LoadedAsync()
        {
            if (!ModernUIHelper.IsInDesignMode &&
                _clientChannel != null)
            {
                IsBusy = true;

                try
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
                    var roster = await _clientChannel.GetResourceAsync<Roster>(
                        _receiveTimeout.ToCancellationToken());

                    this.Contacts.Clear();
                    foreach (var contact in roster.Contacts)
                    {
                        this.Contacts.Add(new ContactViewModel(contact, _clientChannel));
                    }

                    // Sets the presence
                    await _clientChannel.SetResourceAsync(
                        new Presence()
                        {
                            Status = PresenceStatus.Available
                        },
                        _receiveTimeout.ToCancellationToken());

                    _loaded = true;

                }
                catch (Exception ex)
                {
                    base.ErrorMessage = ex.Message;                    
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }

        #endregion
    }
}
