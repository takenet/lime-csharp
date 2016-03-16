using Lime.Client.Windows.Mvvm;
using Lime.Protocol;
using Lime.Protocol.Client;
using Lime.Protocol.Network;
using Lime.Messaging.Resources;
using Lime.Protocol.Security;
using Lime.Protocol.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Serialization.Newtonsoft;
using Lime.Transport.Tcp;
using Microsoft.Win32;

namespace Lime.Client.Windows.ViewModels
{
    public class LoginViewModel : PageViewModelBase, IDataErrorInfo
    {
        #region Private Fields

        private static TimeSpan _loginTimeout = TimeSpan.FromSeconds(30);
        private static TimeSpan _sendTimeout = TimeSpan.FromSeconds(30);

        private static Dictionary<string, string> _knownDomainServers = new Dictionary<string, string>()
        {
            {"0mn.io", "iris.0mn.io"},
            {"limeprotocol.org", "iris.limeprotocol.org"},
            {"mobchat.com.br", "iris.mobchat.com.br"},
        };


        #endregion

        #region Constructor

        public LoginViewModel()
            : base(new Uri("/Pages/Login.xaml", UriKind.Relative))
        {
            LoginCommand = new AsyncCommand(LoginAsync, CanLogin);
            LoadPreferences();
        }

        #endregion

        #region Public properties

        private Node _userNameNode;

        private string _userName;
        public string UserName
        {
            get { return _userName; }
            set
            {
                _userName = value;
                RaisePropertyChanged(() => UserName);
                LoginCommand.RaiseCanExecuteChanged();

                if (_userNameNode != null &&
                    string.IsNullOrWhiteSpace(ServerAddress))
                {
                    // TODO: Do a SRV dns search
                    var domain = _userNameNode.Domain;
                    if (_knownDomainServers.ContainsKey(domain))
                    {
                        domain = _knownDomainServers[domain];
                    }

                    ServerAddress = string.Format("net.tcp://{0}:55321", domain); 
                }
            }
        }

        private string _password;
        public string Password
        {
            get { return _password; }
            set
            {
                _password = value;
                RaisePropertyChanged(() => Password);
                LoginCommand.RaiseCanExecuteChanged();
            }
        }

        private bool _registerUser;
        public bool RegisterUser
        {
            get { return _registerUser; }
            set
            {
                _registerUser = value;
                RaisePropertyChanged(() => RegisterUser);
            }
        }

        private Uri _serverAddressUri;

        private string _serverAddress;
        public string ServerAddress
        {
            get { return _serverAddress; }
            set
            {
                _serverAddress = value;
                RaisePropertyChanged(() => ServerAddress);
                LoginCommand.RaiseCanExecuteChanged();
            }
        }

        private bool _showTraceWindow;
        public bool ShowTraceWindow
        {
            get { return _showTraceWindow; }
            set
            {
                _showTraceWindow = value;
                RaisePropertyChanged(() => ShowTraceWindow);
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
                LoginCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Commands
       
        public AsyncCommand LoginCommand { get; set; } 

        public async Task LoginAsync()
        {
            IClientChannel client = null;

            ITraceWriter traceWriter = null;

            if (ShowTraceWindow)
            {
                traceWriter = Owner.TraceViewModel;

                base.MessengerInstance.Send<OpenWindowMessage>(
                    new OpenWindowMessage()
                    {
                        WindowName = "Trace",
                        DataContext = Owner.TraceViewModel
                    });
            }

            IsBusy = true;
            this.ErrorMessage = string.Empty;

            try
            {
                var cancellationToken = _loginTimeout.ToCancellationToken();

                var transport = new TcpTransport(traceWriter: traceWriter, envelopeSerializer: new JsonNetSerializer());
                await transport.OpenAsync(_serverAddressUri, cancellationToken);

                client = new ClientChannel(
                    transport, 
                    _sendTimeout,
                    fillEnvelopeRecipients: true,
                    autoReplyPings: true,
                    autoNotifyReceipt: true);

                if (RegisterUser)
                {
                    var guestSessionResult = await client.EstablishSessionAsync(
                        compressionOptions => compressionOptions.First(),
                        encryptionOptions => SessionEncryption.TLS,
                        new Identity() { Name = Guid.NewGuid().ToString(), Domain = _userNameNode.Domain },
                        (schemeOptions, roundtrip) => new GuestAuthentication(),
                        null,
                        cancellationToken
                        );

                    if (guestSessionResult.State == SessionState.Established)
                    {
                        // Creates the account
                        var account = new Account()
                        {
                            Password = this.Password.ToBase64()
                        };

                        await client.SetResourceAsync<Account>(
                            LimeUri.Parse(UriTemplates.ACCOUNT),
                            account, 
                            _userNameNode, 
                            cancellationToken);

                        await client.SendFinishingSessionAsync(cancellationToken);
                        await client.ReceiveFinishedSessionAsync(cancellationToken);

                        client.DisposeIfDisposable();

                        transport = new TcpTransport(traceWriter: traceWriter);
                        await transport.OpenAsync(_serverAddressUri, cancellationToken);
                        client = new ClientChannel(
                            transport,
                            _sendTimeout,
                            fillEnvelopeRecipients: true,
                            autoReplyPings: true,
                            autoNotifyReceipt: true);

                    }
                    else if (guestSessionResult.Reason != null)
                    {
                        this.ErrorMessage = guestSessionResult.Reason.Description;
                    }
                    else
                    {
                        this.ErrorMessage = "Could not establish a guest session with the server";
                    }
                }

                var authentication = new PlainAuthentication();
                authentication.SetToBase64Password(this.Password);

                var sessionResult = await client.EstablishSessionAsync(
                    compressionOptions => compressionOptions.First(),
                    encryptionOptions => SessionEncryption.TLS,
                    new Identity() { Name = _userNameNode.Name, Domain = _userNameNode.Domain },
                    (schemeOptions, roundtrip) => authentication,
                    _userNameNode.Instance,
                    cancellationToken);
                
                if (sessionResult.State == SessionState.Established)
                {
                    SavePreferences();

                    var rosterViewModel = new RosterViewModel(client, this);
                    base.Owner.ContentViewModel = rosterViewModel;
                }
                else if (sessionResult.Reason != null)
                {
                    this.ErrorMessage = sessionResult.Reason.Description;
                }
                else
                {
                    this.ErrorMessage = "Could not connect to the server";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                client.DisposeIfDisposable();
            }
            finally
            {
                IsBusy = false;
            }            
        }

        public bool CanLogin()
        {            
            return !IsBusy && 
                   Node.TryParse(UserName, out _userNameNode) &&
                   !string.IsNullOrWhiteSpace(Password) &&
                   Uri.TryCreate(_serverAddress, UriKind.Absolute, out _serverAddressUri);
        }

        #endregion

        #region IDataErrorInfo Members

        public string Error
        {
            get { return null; }
        }

        public string this[string columnName]
        {
            get
            {
                if (columnName == "UserName")
                {
                    if (!string.IsNullOrWhiteSpace(_userName) &&
                        !System.Text.RegularExpressions.Regex.IsMatch(_userName, @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z"))
                    {
                        return "Formato inválido";
                    }

                    return null;
                }
                return null;
            }
        }

        #endregion

        private void LoadPreferences()
        {
            var key = Registry.CurrentUser.CreateSubKey("Lime Messenger");
            if (key != null)
            {
                UserName = key.GetValue(nameof(UserName)) as string;
                Password = key.GetValue(nameof(Password)) as string;
                ServerAddress = key.GetValue(nameof(ServerAddress)) as string;                
                var showTraceWindowValue = key.GetValue(nameof(ShowTraceWindow)) as string;
                bool showTraceWindow;
                if (showTraceWindowValue != null && bool.TryParse(showTraceWindowValue, out showTraceWindow))
                {
                    ShowTraceWindow = showTraceWindow;
                }

            }
        }


        private void SavePreferences()
        {
            var key = Registry.CurrentUser.CreateSubKey("Lime Messenger");
            if (key != null)
            {
                key.SetValue(nameof(UserName), UserName);
                key.SetValue(nameof(Password), Password);
                key.SetValue(nameof(ServerAddress), ServerAddress);
                key.SetValue(nameof(ShowTraceWindow), ShowTraceWindow.ToString());
                key.Close();
            }
        }

    }
}
