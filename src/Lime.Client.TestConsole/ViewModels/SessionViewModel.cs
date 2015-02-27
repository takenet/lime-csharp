using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Lime.Client.TestConsole.Macros;
using Lime.Client.TestConsole.Mvvm;
using Lime.Client.TestConsole.Properties;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using Lime.Transport.Tcp;

namespace Lime.Client.TestConsole.ViewModels
{
    public class SessionViewModel : ViewModelBase, ITraceWriter
    {
        #region Private Fields

        private readonly TimeSpan _operationTimeout;

        #endregion

        #region Constructor

        public SessionViewModel()
        {
            _operationTimeout = TimeSpan.FromSeconds(15);

            // Collections
            this.Envelopes = new ObservableCollectionEx<EnvelopeViewModel>();
            this.Variables = new ObservableCollectionEx<VariableViewModel>();
            this.Templates = new ObservableCollectionEx<TemplateViewModel>();
            this.Macros = new ObservableCollectionEx<MacroViewModel>();
            this.StatusMessages = new ObservableCollectionEx<StatusMessageViewModel>();

            // Commands
            this.OpenTransportCommand = new AsyncCommand(OpenTransportAsync, CanOpenTransport);
            this.CloseTransportCommand = new AsyncCommand(CloseTransportAsync, CanCloseTransport);
            this.SendCommand = new AsyncCommand(SendAsync, CanSend);
            this.ClearTraceCommand = new RelayCommand(ClearTrace);
            this.IndentCommand = new RelayCommand(Indent, CanIndent);
            this.ValidateCommand = new RelayCommand(Validate, CanValidate);
            this.LoadTemplateCommand = new RelayCommand(LoadTemplate, CanLoadTemplate);
            this.ParseCommand = new RelayCommand(Parse, CanParse);

            this.Host = "net.tcp://iris.limeprotocol.org:55321";
            this.ClientCertificateThumbprint = Settings.Default.LastCertificateThumbprint;
            this.ClearAfterSent = true;
            this.ParseBeforeSend = true;

            if (!IsInDesignMode)
            {
                LoadHost();
                LoadVariables();
                LoadTemplates();
                LoadMacros();
            }
        }


        #endregion

        #region Data Properties

        private ITcpClient _tcpClient;

        public ITcpClient TcpClient
        {
            get { return _tcpClient; }
            set 
            { 
                _tcpClient = value;
                RaisePropertyChanged(() => TcpClient);
            }
        }

        private ITransport _transport;

        public ITransport Transport
        {
            get { return _transport; }
            set 
            { 
                _transport = value;
                RaisePropertyChanged(() => Transport);
            }
        }


        private bool _isBusy;
        public bool IsBusy
        {
            get { return _isBusy; }
            set 
            { 
                _isBusy = value;
                RaisePropertyChanged(() => IsBusy);

                OpenTransportCommand.RaiseCanExecuteChanged();
                CloseTransportCommand.RaiseCanExecuteChanged();
                SendCommand.RaiseCanExecuteChanged();
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

        private ObservableCollectionEx<StatusMessageViewModel> _statusMessages;

        public ObservableCollectionEx<StatusMessageViewModel> StatusMessages
        {
            get { return _statusMessages; }
            set 
            { 
                _statusMessages = value;
                RaisePropertyChanged(() => StatusMessages);
            }
        }

        private string _clientCertificateThumbprint;

        public string ClientCertificateThumbprint
        {
            get { return _clientCertificateThumbprint; }
            set 
            { 
                _clientCertificateThumbprint = value;
                Settings.Default.LastCertificateThumbprint = value;
                RaisePropertyChanged(() => ClientCertificateThumbprint);
            }
        }

        private SessionState _lastSessionState;

        public SessionState LastSessionState
        {
            get { return _lastSessionState; }
            set 
            { 
                _lastSessionState = value;
                RaisePropertyChanged(() => LastSessionState);
            }
        }

        private Node _localNode;
        public Node LocalNode
        {
            get { return _localNode; }
            set 
            { 
                _localNode = value;
                RaisePropertyChanged(() => LocalNode);
            }
        }

        private Node _remoteNode;

        public Node RemoteNode
        {
            get { return _remoteNode; }
            set 
            { 
                _remoteNode = value;
                RaisePropertyChanged(() => RemoteNode);
            }
        }



        private Event? _lastNotificationEvent;

        public Event? LastNotificationEvent
        {
            get { return _lastNotificationEvent; }
            set 
            { 
                _lastNotificationEvent = value;
                RaisePropertyChanged(() => LastNotificationEvent);
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
                ParseCommand.RaiseCanExecuteChanged();
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

                if (_envelopes != null)
                {
                    EnvelopesView = CollectionViewSource.GetDefaultView(_envelopes);
                    EnvelopesView.Filter = new Predicate<object>(o =>
                    {
                        var envelopeViewModel = o as EnvelopeViewModel;

                        return envelopeViewModel != null &&
                               (ShowRawValues || !envelopeViewModel.IsRaw);
                    });
                }
            }
        }

        private ICollectionView _envelopesView;

        public ICollectionView EnvelopesView
        {
            get { return _envelopesView; }
            set 
            { 
                _envelopesView = value;
                RaisePropertyChanged(() => EnvelopesView);
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

                if (EnvelopesView != null)
                {
                    EnvelopesView.Refresh();
                }
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

        private bool _canSendAsRaw;

        public bool CanSendAsRaw
        {
            get { return _canSendAsRaw; }
            set 
            { 
                _canSendAsRaw = value;
                RaisePropertyChanged(() => CanSendAsRaw);
            }
        }

        private bool _parseBeforeSend;

        public bool ParseBeforeSend
        {
            get { return _parseBeforeSend; }
            set 
            { 
                _parseBeforeSend = value;
                RaisePropertyChanged(() => ParseBeforeSend);
            }
        }

        private bool _clearAfterSent;

        public bool ClearAfterSent
        {
            get { return _clearAfterSent; }
            set 
            { 
                _clearAfterSent = value;
                RaisePropertyChanged(() => ClearAfterSent);
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


        private TemplateViewModel _selectedTemplate;

        public TemplateViewModel SelectedTemplate
        {
            get { return _selectedTemplate; }
            set 
            { 
                _selectedTemplate = value;
                RaisePropertyChanged(() => SelectedTemplate);

                LoadTemplateCommand.RaiseCanExecuteChanged();
            }
        }


        private ObservableCollectionEx<MacroViewModel> _macros;

        public ObservableCollectionEx<MacroViewModel> Macros
        {
            get { return _macros; }
            set 
            { 
                _macros = value;
                RaisePropertyChanged(() => Macros);

                if (_macros != null)
                {
                    MacrosView = CollectionViewSource.GetDefaultView(_macros);
                    MacrosView.GroupDescriptions.Add(new PropertyGroupDescription("Category"));
                    MacrosView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));

                    RaisePropertyChanged(() => TemplatesView);
                }
            }
        }

        public ICollectionView MacrosView { get; private set; }


        private MacroViewModel _selectedMacro;

        public MacroViewModel SelectedMacro
        {
            get { return _selectedMacro; }
            set 
            { 
                _selectedMacro = value;
                RaisePropertyChanged(() => SelectedMacro);
            }
        }

        #endregion

        #region Commands

        public AsyncCommand OpenTransportCommand { get; private set; }

        private async Task OpenTransportAsync()
        {
            await ExecuteAsync(async () =>
                {
                    AddStatusMessage("Connecting...");

                    var timeoutCancellationToken = _operationTimeout.ToCancellationToken();

                    X509Certificate2 clientCertificate = null;

                    if (!string.IsNullOrWhiteSpace(ClientCertificateThumbprint))
                    {
                        ClientCertificateThumbprint = ClientCertificateThumbprint
                            .Replace(" ", "")
                            .Replace("‎", "");

                        var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);

                        try
                        {
                            store.Open(OpenFlags.ReadOnly);

                            var certificates = store.Certificates.Find(X509FindType.FindByThumbprint, ClientCertificateThumbprint, false);
                            if (certificates.Count > 0)
                            {
                                clientCertificate = certificates[0];

                                var identity = clientCertificate.GetIdentity();

                                if (identity != null)
                                {
                                    var fromVariableViewModel = this.Variables.FirstOrDefault(v => v.Name.Equals("from", StringComparison.OrdinalIgnoreCase));

                                    if (fromVariableViewModel == null)
                                    {
                                        fromVariableViewModel = new VariableViewModel()
                                        {
                                            Name = "from"
                                        };

                                        this.Variables.Add(fromVariableViewModel);
                                    }

                                    fromVariableViewModel.Value = identity.ToString();
                                }

                            }
                            else
                            {
                                AddStatusMessage("The specified certificate was not found", true);
                            }
                        }
                        finally
                        {
                            store.Close();
                        }                        
                    }

                    TcpClient = new TcpClientAdapter(new TcpClient());

                    Transport = new TcpTransport(
                        TcpClient,
                        new EnvelopeSerializer(),
                        _hostUri.Host,
                        clientCertificate: clientCertificate,
                        traceWriter: this);

                    await Transport.OpenAsync(_hostUri, timeoutCancellationToken);

                    _connectionCts = new CancellationTokenSource();

                    var dispatcher = Dispatcher.CurrentDispatcher;
                    
                    _receiveTask = ReceiveAsync(
                        Transport,
                        (e) => ReceiveEnvelopeAsync(e, dispatcher),
                        _connectionCts.Token)
                    .WithCancellation(_connectionCts.Token)
                    .ContinueWith(t => 
                    {
                        IsConnected = false;

                        if (t.Exception != null)
                        {
                            AddStatusMessage(string.Format("Disconnected with errors: {0}", t.Exception.InnerException.Message.RemoveCrLf()), true);
                        }
                        else
                        {
                            AddStatusMessage("Disconnected");
                        }

                    }, TaskScheduler.FromCurrentSynchronizationContext());

                    IsConnected = true;
                    CanSendAsRaw = true;

                    AddStatusMessage("Connected");

                });
        }

        private bool CanOpenTransport()
        {
            return 
                !IsBusy &&
                !IsConnected &&
                Uri.TryCreate(_host, UriKind.Absolute, out _hostUri);
        }

        public AsyncCommand CloseTransportCommand { get; private set; }

        private async Task CloseTransportAsync()
        {
            await ExecuteAsync(async () =>
                {
                    AddStatusMessage("Disconnecting...");

                    var timeoutCancellationToken = _operationTimeout.ToCancellationToken();

                    _connectionCts.Cancel();                    

                    // Closes the transport
                    await Transport.CloseAsync(timeoutCancellationToken);
                    await _receiveTask.WithCancellation(timeoutCancellationToken);
                    
                    Transport.DisposeIfDisposable();
                    Transport = null;                    
                });
        }

        private bool CanCloseTransport()
        {
            return 
                !IsBusy && 
                IsConnected;
        }


        public RelayCommand IndentCommand { get; private set; }

        private void Indent()
        {
            Execute(() =>
                {
                    InputJson = InputJson.IndentJson();
                });
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
                        AddStatusMessage(string.Format("The input is a valid {0} JSON Envelope", envelopeViewModel.Envelope.GetType().Name));
                    }
                    else
                    {
                        AddStatusMessage("The input is a valid JSON document, but is not an Envelope", true);
                    }

                    var variables = InputJson.GetVariables();

                    foreach (var variable in variables)
                    {
                        if (!this.Variables.Any(v => v.Name.Equals(variable, StringComparison.OrdinalIgnoreCase)))
                        {
                            var variableViewModel = new VariableViewModel()
                            {
                                Name = variable
                            };

                            this.Variables.Add(variableViewModel);
                        }
                    }
                }
                else
                {
                    AddStatusMessage("The input is a invalid or empty JSON document", true);
                }
            }
            catch (ArgumentException)
            {
                AddStatusMessage("The input is a invalid JSON document", true);
            }
        }

        private bool CanValidate()
        {
            return !string.IsNullOrWhiteSpace(InputJson);
        }

        public RelayCommand ParseCommand { get; private set; }

        private void Parse()
        {
            Execute(() =>
                {
                    InputJson = ParseInput(InputJson, Variables);
                });
        }
      
        private bool CanParse()
        {
            return !string.IsNullOrWhiteSpace(InputJson);
        }

        public AsyncCommand SendCommand { get; private set; }


        private async Task SendAsync()
        {
            await ExecuteAsync(async () =>
                {
                    AddStatusMessage("Sending...");

                    if (ParseBeforeSend)
                    {
                        this.InputJson = ParseInput(this.InputJson, this.Variables);
                    }

                    var timeoutCancellationToken = _operationTimeout.ToCancellationToken();

                    var envelopeViewModel = new EnvelopeViewModel(false);
                    envelopeViewModel.Json = InputJson;
                    var envelope = envelopeViewModel.Envelope;
                    envelopeViewModel.Direction = DataOperation.Send;

                    if (SendAsRaw)
                    {
                        envelopeViewModel.IsRaw = true;
                        var stream = TcpClient.GetStream();
                        var envelopeBytes = Encoding.UTF8.GetBytes(envelopeViewModel.Json);
                        await stream.WriteAsync(envelopeBytes, 0, envelopeBytes.Length, timeoutCancellationToken);
                    }
                    else
                    {
                        await Transport.SendAsync(envelope, timeoutCancellationToken);
                        envelopeViewModel.IndentJson();
                    }

                    this.Envelopes.Add(envelopeViewModel);

                    if (this.ClearAfterSent)
                    {
                        this.InputJson = string.Empty;
                    }

                    AddStatusMessage(string.Format("{0} envelope sent", envelope.GetType().Name));
                });
        }

        private bool CanSend()
        {
            return 
                !IsBusy &&
                IsConnected && 
                !string.IsNullOrWhiteSpace(InputJson);
        }

        public RelayCommand ClearTraceCommand { get; private set; }
        
        private void ClearTrace()
        {
            this.Envelopes.Clear();
        }


        public RelayCommand LoadTemplateCommand { get; private set; }

        private void LoadTemplate()
        {
            this.InputJson = SelectedTemplate.JsonTemplate.IndentJson();
            this.Validate();
            AddStatusMessage("Template loaded");
        }

        private bool CanLoadTemplate()
        {
            return SelectedTemplate != null;
        }

        #endregion

        #region Private Methods

        private void Execute(Action action)
        {
            IsBusy = true;

            try
            {
                action();
            }
            catch (Exception ex)
            {
                AddStatusMessage(ex.Message.RemoveCrLf());
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExecuteAsync(Func<Task> func)
        {
            IsBusy = true;

            try
            {
                await func();
            }
            catch (Exception ex)
            {
                AddStatusMessage(ex.Message.RemoveCrLf(), true);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private Task _receiveTask;
        private CancellationTokenSource _connectionCts;

        private static async Task ReceiveAsync(ITransport transport, Func<Envelope, Task> processFunc, CancellationToken cancellationToken)
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
                    await processFunc(envelope);
                }
            }
            catch (OperationCanceledException) { }            
        }

        public const string HOST_FILE_NAME = "Host.txt";

        private void LoadHost()
        {
            if (File.Exists(HOST_FILE_NAME))
            {
                Host = File.ReadAllText(HOST_FILE_NAME);
            }
        }

        private void SaveHost()
        {
            if (!string.IsNullOrEmpty(Host))
            {
                File.WriteAllText(HOST_FILE_NAME, Host);
            }
        }

        public const string TEMPLATES_FILE_NAME = "Templates.txt";
        public const char TEMPLATES_FILE_SEPARATOR = '\t';

        private void LoadTemplates()
        {
            foreach (var lineValues in FileUtil.GetFileLines(TEMPLATES_FILE_NAME, TEMPLATES_FILE_SEPARATOR))
            {
                if (lineValues.Length >= 3)
                {
                    var templateViewModel = new TemplateViewModel()
                    {
                        Name = lineValues[0],
                        Category = lineValues[1],
                        JsonTemplate = lineValues[2]
                    };

                    this.Templates.Add(templateViewModel);
                }
            }

        }

        public const string VARIABLES_FILE_NAME = "Variables.txt";
        public const char VARIABLES_FILE_SEPARATOR = '\t';

        private void LoadVariables()
        {
            foreach (var lineValues in FileUtil.GetFileLines(VARIABLES_FILE_NAME, VARIABLES_FILE_SEPARATOR))
            {
                if (lineValues.Length >= 2)
                {
                    var variableViewModel = new VariableViewModel()
                    {
                        Name = lineValues[0],
                        Value = lineValues[1]
                    };

                    Variables.Add(variableViewModel);
                }
            }
        }

        private void SaveVariables()
        {
            var lineValues = Variables.Select(v => new [] {v.Name, v.Value});
            FileUtil.SaveFile(lineValues, VARIABLES_FILE_NAME, VARIABLES_FILE_SEPARATOR);            

        }

        private static string ParseInput(string input, IEnumerable<VariableViewModel> variables)
        {
            var variableValues = variables.ToDictionary(t => t.Name, t => t.Value);
            variableValues.Add("newGuid", Guid.NewGuid().ToString());
            return input.ReplaceVariables(variableValues);
        }

        private void LoadMacros()
        {
            var macroTypes = Assembly
                .GetExecutingAssembly()
                .GetTypes()
                .Where(t => typeof(IMacro).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract);

            foreach (var type in macroTypes)
            {
                var macroViewModel = new MacroViewModel()
                {
                    Type = type
                };

                Macros.Add(macroViewModel);
            }
        }

        private async Task ReceiveEnvelopeAsync(Envelope envelope, Dispatcher dispatcher)
        {
            var envelopeViewModel = new EnvelopeViewModel
            {
                Envelope = envelope, 
                Direction = DataOperation.Receive
            };

            await await dispatcher.InvokeAsync(async () => 
                {
                    Envelopes.Add(envelopeViewModel);

                    foreach (var macro in Macros.Where(m => m.IsActive))
                    {
                        await macro.Macro.ProcessAsync(envelopeViewModel, this);
                    }
                });
        }

        private void AddStatusMessage(string message, bool isError = false)
        {
            StatusMessage = message;

            var statusMessageViewModel = new StatusMessageViewModel()
            {
                Timestamp = DateTimeOffset.Now,
                Message = message,
                IsError  = isError
            };

            StatusMessages.Add(statusMessageViewModel);
        }

        #endregion

        #region ITraceWriter Members

        public async Task TraceAsync(string data, DataOperation operation)
        {
            var envelopeViewModel = new EnvelopeViewModel(false)
            {
                IsRaw = true,
                Json = data,
                Direction = operation
            };

            await App.Current.Dispatcher.InvokeAsync(() => this.Envelopes.Add(envelopeViewModel));
        }

        public bool IsEnabled
        {
            get { return ShowRawValues; }
        }

        #endregion

        public void SavePreferences()
        {
            if (!IsInDesignMode)
            {
                SaveHost();
                SaveVariables();                
            }
        }

    }

    public static class VariablesExtensions
    {
        private static Regex _variablesRegex = new Regex(@"(?<=%)(\w+)", RegexOptions.Compiled);
        private static string _variablePatternFormat = @"\B%{0}\b";

        public static IEnumerable<string> GetVariables(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                throw new ArgumentNullException("input");
            }

            foreach (Match match in _variablesRegex.Matches(input))
            {
                yield return match.Value;
            }
        }

        public static string ReplaceVariables(this string input, Dictionary<string, string> variableValues)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                throw new ArgumentNullException("input");
            }

            if (variableValues == null)
            {
                throw new ArgumentNullException("variableValues");
            }

            var variableNames = input.GetVariables();

            foreach (var variableName in variableNames)
            {
                string variableValue;

                if (!variableValues.TryGetValue(variableName, out variableValue))
                {
                    throw new ArgumentException(string.Format("The variable '{0}' is not present", variableName));
                }

                if (string.IsNullOrWhiteSpace(variableValue))
                {
                    throw new ArgumentException(string.Format("The value of the variable '{0}' is empty", variableName));
                }

                int deepth = 0;

                while (variableValue.StartsWith("%"))
                {
                    var innerVariableName = variableValue.TrimStart('%');

                    if (!variableValues.TryGetValue(innerVariableName, out variableValue))
                    {
                        throw new ArgumentException(string.Format("The variable '{0}' is not present", innerVariableName));
                    }

                    deepth++;

                    if (deepth > 10)
                    {
                        throw new ArgumentException("Deepth variable limit reached");
                    }
                }

                var variableRegex = new Regex(string.Format(_variablePatternFormat, variableName));
                input = variableRegex.Replace(input, variableValue);                
            }

            return input;
        }
    }
}
