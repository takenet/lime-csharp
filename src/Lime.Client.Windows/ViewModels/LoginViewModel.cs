using Lime.Client.Windows.Mvvm;
using Lime.Protocol;
using Lime.Protocol.Client;
using Lime.Protocol.Network;
using Lime.Protocol.Security;
using Lime.Protocol.Tcp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Client.Windows.ViewModels
{
    public class LoginViewModel : PageViewModelBase, IDataErrorInfo
    {
        #region Private Fields

        private static TimeSpan _loginTimeout = TimeSpan.FromSeconds(30);
        private static TimeSpan _sendTimeout = TimeSpan.FromSeconds(30);

        #endregion

        #region Constructor

        public LoginViewModel()
            : base(new Uri("/Pages/Login.xaml", UriKind.Relative))
        {
            LoginCommand = new AsyncCommand(LoginAsync, CanLogin);

#if DEBUG
            ServerAddress = string.Format("net.tcp://{0}:55321", Dns.GetHostEntry("localhost").HostName); 
#endif
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
                    ServerAddress = string.Format("net.tcp://{0}:55321", _userNameNode.Domain); 
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

                var transport = new TcpTransport(traceWriter: traceWriter);
                await transport.OpenAsync(_serverAddressUri, cancellationToken);

                var client = new ClientChannel(transport, _sendTimeout);

                var authentication = new PlainAuthentication();
                authentication.SetToBase64Password(this.Password);

                var sessionResult = await client.EstablishSessionAsync(
                    compressionOptions => compressionOptions.First(),
                    encryptionOptions => SessionEncryption.TLS,
                    new Identity() { Name = _userNameNode.Name, Domain = _userNameNode.Domain },
                    (schemeOptions, roundtrip) => authentication,
                    _userNameNode.Instance,
                    SessionMode.Node,
                    cancellationToken);
                
                if (sessionResult.State == SessionState.Established)
                {
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
    }
}
