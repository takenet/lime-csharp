using Lime.Client.Windows.Mvvm;
using Lime.Protocol;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Client.Windows.ViewModels
{
    public class LoginViewModel : PageViewModelBase, IDataErrorInfo
    {
        #region Constructor

        public LoginViewModel()
            : base(new Uri("/Pages/Login.xaml", UriKind.Relative))
        {
            LoginCommand = new AsyncCommand(p => LoginAsync(), p => CanLogin());

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

        #endregion

        #region Commands
        public AsyncCommand LoginCommand { get; set; } 

        public async Task LoginAsync()
        {
            IsBusy = true;

            await Task.Delay(5000);

            IsBusy = false;
        }

        public bool CanLogin()
        {
            Uri uri;

            return Node.TryParse(UserName, out _userNameNode) &&
                   !string.IsNullOrWhiteSpace(Password) &&
                   Uri.TryCreate(_serverAddress, UriKind.Absolute, out uri);
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
