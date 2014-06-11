using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Lime.Client.TestConsole.Mvvm;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Lime.Protocol.Tcp;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;

namespace Lime.Client.TestConsole.ViewModels
{
    public class SessionViewModel : ViewModelBase
    {
        #region Private Fields

        private readonly TimeSpan _operationTimeout;
        private ITransport _transport;

        #endregion

        #region Constructor

        public SessionViewModel()
        {
            _operationTimeout = TimeSpan.FromSeconds(60);


            this.Envelopes = new ObservableCollectionEx<EnvelopeViewModel>();


            this.Envelopes.CollectionChanged += Envelopes_CollectionChanged;

            this.Variables = new ObservableCollectionEx<VariableViewModel>();
            this.Templates = new ObservableCollectionEx<TemplateViewModel>();

            this.OpenTransportCommand = new AsyncCommand(OpenTransportAsync, CanOpenTransport);
            this.CloseTransportCommand = new AsyncCommand(CloseTransportAsync, CanCloseTransport);

            this.SendCommand = new AsyncCommand(SendAsync, CanSend);
            this.IndentCommand = new RelayCommand(Indent, CanIndent);
            this.ValidateCommand = new RelayCommand(Validate, CanValidate);

            this.AvailableTransports = new[] { "Tcp" };
            this.SelectedTransport = AvailableTransports.First();

            this.Host = "net.tcp://localhost:55321";
        }

        #endregion

        #region Data Properties


        private bool _isBusy;
        public bool IsBusy
        {
            get { return _isBusy; }
            set 
            { 
                _isBusy = value;
                RaisePropertyChanged(() => IsBusy);
            }
        }

        private string _statusMessage;

        public string StatusMessage
        {
            get { return _statusMessage; }
            set 
            { 
                _statusMessage = value;
                RaisePropertyChanged(() => StatusMessage);
            }
        }


        private IEnumerable<string> _availableTransports;

        public IEnumerable<string> AvailableTransports
        {
            get { return _availableTransports; }
            set 
            { 
                _availableTransports = value;
                RaisePropertyChanged(() => AvailableTransports);
            }
        }

        private string _selectedTransport;

        public string SelectedTransport
        {
            get { return _selectedTransport; }
            set 
            { 
                _selectedTransport = value;
                RaisePropertyChanged(() => SelectedTransport);               
                OpenTransportCommand.RaiseCanExecuteChanged();
            }
        }

        private string _inputJson;

        public string InputJson
        {
            get { return _inputJson; }
            set
            {
                _inputJson = value;
                RaisePropertyChanged(() => InputJson);

                SendCommand.RaiseCanExecuteChanged();
                IndentCommand.RaiseCanExecuteChanged();
                ValidateCommand.RaiseCanExecuteChanged();
            }
        }

        private string _host;

        private Uri _hostUri;

        public string Host
        {
            get { return _host; }
            set
            {
                _host = value;
                RaisePropertyChanged(() => Host);

                OpenTransportCommand.RaiseCanExecuteChanged();
            }
        }


        private bool _isConnected;

        public bool IsConnected
        {
            get { return _isConnected; }
            set 
            { 
                _isConnected = value;
                RaisePropertyChanged(() => IsConnected);

                OpenTransportCommand.RaiseCanExecuteChanged();
                CloseTransportCommand.RaiseCanExecuteChanged();
                SendCommand.RaiseCanExecuteChanged();
            }
        }

        private ObservableCollectionEx<EnvelopeViewModel> _envelopes;

        public ObservableCollectionEx<EnvelopeViewModel> Envelopes
        {
            get { return _envelopes; }
            set
            {
                _envelopes = value;
                RaisePropertyChanged(() => Envelopes);
            }
        }

        private bool _showRawValues;

        public bool ShowRawValues
        {
            get { return _showRawValues; }
            set 
            { 
                _showRawValues = value;
                RaisePropertyChanged(() => ShowRawValues);
            }
        }

        private bool _sendAsRaw;

        public bool SendAsRaw
        {
            get { return _sendAsRaw; }
            set
            {
                _sendAsRaw = value;
                RaisePropertyChanged(() => SendAsRaw);
            }
        }

        private ObservableCollectionEx<VariableViewModel> _variables;

        public ObservableCollectionEx<VariableViewModel> Variables
        {
            get { return _variables; }
            set 
            { 
                _variables = value;
                RaisePropertyChanged(() => Variables);
            }
        }



        private ObservableCollectionEx<TemplateViewModel> _templates;

        public ObservableCollectionEx<TemplateViewModel> Templates
        {
            get { return _templates; }
            set 
            { 
                _templates = value;
                RaisePropertyChanged(() => Templates);

                if (_templates != null)
                {
                    TemplatesView = CollectionViewSource.GetDefaultView(_templates);
                    TemplatesView.GroupDescriptions.Add(new PropertyGroupDescription("Category"));
                    TemplatesView.SortDescriptions.Add(new SortDescription("SortOrder", ListSortDirection.Ascending));

                    RaisePropertyChanged(() => TemplatesView);                    
                }
            }
        }

        public ICollectionView TemplatesView { get; private set; }

        #endregion

        #region Commands

        public AsyncCommand OpenTransportCommand { get; private set; }


        private async Task OpenTransportAsync()
        {
            await ExecuteAsync(async () =>
                {
                    if (_transport != null)
                    {
                        _transport.DisposeIfDisposable();
                        _transport = null;
                    }

                    var timeoutCancellationToken = _operationTimeout.ToCancellationToken();

                    _transport = new TcpTransport();                    

                    _transport.Closed += (sender, e) =>
                        {
                            Dispatcher.CurrentDispatcher.Invoke(() => IsConnected = false);
                        };

                    await _transport.OpenAsync(_hostUri, timeoutCancellationToken);

                    _connectionCts = new CancellationTokenSource();

                    _receiveTask = ReceiveAsync(
                        _transport,
                        e =>
                        {
                            var envelopeViewModel = new EnvelopeViewModel();
                            envelopeViewModel.Envelope = e;
                            envelopeViewModel.Direction = DataOperation.Receive;

                            Dispatcher.CurrentDispatcher.Invoke(() => 
                                {
                                    this.Envelopes.Add(envelopeViewModel);                                    
                                });
                        },
                        _connectionCts.Token);

                    IsConnected = true;

                    StatusMessage = "Connected";

                });
        }

        private bool CanOpenTransport()
        {
            return !IsConnected &&
                   SelectedTransport != null && 
                   Uri.TryCreate(_host, UriKind.Absolute, out _hostUri);
        }

        public AsyncCommand CloseTransportCommand { get; private set; }

        private async Task CloseTransportAsync()
        {
            await ExecuteAsync(async () =>
                {
                    var timeoutCancellationToken = _operationTimeout.ToCancellationToken();

                    _connectionCts.Cancel();

                    await _receiveTask;

                    // Closes the transport. The IsConnected property
                    // should be changed by the action attached to the
                    // transport Closing event
                    await _transport.CloseAsync(timeoutCancellationToken);

                    StatusMessage = "Disconnected";
                });
        }

        private bool CanCloseTransport()
        {
            return IsConnected;
        }


        public RelayCommand IndentCommand { get; private set; }

        private void Indent()
        {
            try 
	        {
                InputJson = InputJson.IndentJson();                 
	        }
	        catch (Exception ex)
	        {
                StatusMessage = ex.Message;
	        }
        }

        private bool CanIndent()
        {
            return !string.IsNullOrWhiteSpace(InputJson);
        }


        public RelayCommand ValidateCommand { get; private set; }

        private void Validate()
        {
            try
            {
                var jsonObject = JsonObject.ParseJson(InputJson);

                if (jsonObject.Any())
                {
                    var envelopeViewModel = new EnvelopeViewModel();
                    envelopeViewModel.Json = InputJson;

                    if (envelopeViewModel.Envelope != null)
                    {
                        StatusMessage = string.Format("The input is a valid {0} JSON Envelope", envelopeViewModel.Envelope.GetType().Name);
                    }
                    else
                    {
                        StatusMessage = "The input is a valid JSON document, but is not an Envelope";
                    }
                }
                else
                {
                    StatusMessage = "The input is a invalid or empty JSON document";
                }
            }
            catch (ArgumentException)
            {
                StatusMessage = "The input is a invalid JSON document";
            }
        }

        private bool CanValidate()
        {
            return !string.IsNullOrWhiteSpace(InputJson);
        }

        public AsyncCommand SendCommand { get; private set; }


        private async Task SendAsync()
        {
            await ExecuteAsync(async () =>
                {
                    var timeoutCancellationToken = _operationTimeout.ToCancellationToken();


                    var envelopeViewModel = new EnvelopeViewModel();
                    envelopeViewModel.Json = InputJson;
                    var envelope = envelopeViewModel.Envelope;
                    envelopeViewModel.Direction = DataOperation.Send;

                    await _transport.SendAsync(envelope, timeoutCancellationToken);

                    this.Envelopes.Add(envelopeViewModel);
                });
        }

        private bool CanSend()
        {
            return IsConnected && !string.IsNullOrWhiteSpace(InputJson);
        }

        #endregion

        #region Private Methods

        private async Task ExecuteAsync(Func<Task> func)
        {
            IsBusy = true;

            try
            {
                await func();
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message.RemoveCrLf();
            }
            finally
            {
                IsBusy = false;
            }
        }


        private Task _receiveTask;
        private CancellationTokenSource _connectionCts;

        private static async Task ReceiveAsync(ITransport transport, Action<Envelope> processAction, CancellationToken cancellationToken)
        {
            if (transport == null)
            {
                throw new ArgumentNullException("transport");
            }

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var envelope = await transport.ReceiveAsync(cancellationToken);
                    processAction(envelope);
                }
            }
            catch (OperationCanceledException) { }
        }


        private async void Envelopes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            
        }

        #endregion

    }
}
