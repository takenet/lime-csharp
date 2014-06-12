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
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

            // Collections
            this.Envelopes = new ObservableCollectionEx<EnvelopeViewModel>();
            this.Envelopes.CollectionChanged += Envelopes_CollectionChanged;
            this.Variables = new ObservableCollectionEx<VariableViewModel>();
            this.Templates = new ObservableCollectionEx<TemplateViewModel>();

            // Commands
            this.OpenTransportCommand = new AsyncCommand(OpenTransportAsync, CanOpenTransport);
            this.CloseTransportCommand = new AsyncCommand(CloseTransportAsync, CanCloseTransport);
            this.SendCommand = new AsyncCommand(SendAsync, CanSend);
            this.IndentCommand = new RelayCommand(Indent, CanIndent);
            this.ValidateCommand = new RelayCommand(Validate, CanValidate);
            this.LoadTemplateCommand = new RelayCommand(LoadTemplate, CanLoadTemplate);
            this.ParseCommand = new RelayCommand(Parse, CanParse);

            this.AvailableTransports = new[] { "Tcp" };
            this.SelectedTransport = AvailableTransports.First();

            this.Host = "net.tcp://iris.limeprotocol.org:55321";
            this.ClearAfterSent = true;

            LoadVariables();
            LoadTemplates();
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
                        _connectionCts.Token)
                    .ContinueWith(t => 
                    {
                        IsConnected = false;

                        if (t.Exception != null)
                        {
                            this.StatusMessage = t.Exception.Message.RemoveCrLf();
                        }
                    },
                    TaskScheduler.FromCurrentSynchronizationContext());

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

        public RelayCommand ParseCommand { get; private set; }

        private void Parse()
        {
            var variableValues = this.Variables.ToDictionary(t => t.Name, t => t.Value);
            InputJson = InputJson.ReplaceVariables(variableValues);
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
                    this.Parse();

                    var timeoutCancellationToken = _operationTimeout.ToCancellationToken();

                    var envelopeViewModel = new EnvelopeViewModel();
                    envelopeViewModel.Json = InputJson;
                    var envelope = envelopeViewModel.Envelope;
                    envelopeViewModel.Direction = DataOperation.Send;

                    await _transport.SendAsync(envelope, timeoutCancellationToken);

                    this.Envelopes.Add(envelopeViewModel);

                    if (this.ClearAfterSent)
                    {
                        this.InputJson = string.Empty;
                    }
                });
        }

        private bool CanSend()
        {
            return IsConnected && !string.IsNullOrWhiteSpace(InputJson);
        }


        public RelayCommand LoadTemplateCommand { get; private set; }

        private void LoadTemplate()
        {
            this.InputJson = SelectedTemplate.JsonTemplate.IndentJson();
            this.Validate();
            this.StatusMessage = "Template loaded";
        }

        private bool CanLoadTemplate()
        {
            return SelectedTemplate != null;
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

        public const string TEMPLATES_FILE_NAME = "Templates.txt";
        public const char TEMPLATES_FILE_SEPARATOR = '\t';

        private void LoadTemplates()
        {
            foreach (var lineValues in GetFileLines(TEMPLATES_FILE_NAME, TEMPLATES_FILE_SEPARATOR))
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
            foreach (var lineValues in GetFileLines(VARIABLES_FILE_NAME, VARIABLES_FILE_SEPARATOR))
            {
                if (lineValues.Length >= 2)
                {
                    var variableViewModel = new VariableViewModel()
                    {
                        Name = lineValues[0],
                        Value = lineValues[1]
                    };

                    this.Variables.Add(variableViewModel);
                }
            }
        }

        private IEnumerable<string[]> GetFileLines(string fileName, char separator)
        {
            using (var fileStream = File.Open(fileName, FileMode.OpenOrCreate, FileAccess.Read))
            {
                using (var streamReader = new StreamReader(fileStream))
                {
                    while (!streamReader.EndOfStream)
                    {
                        var line = streamReader.ReadLine();

                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            yield return line.Split(separator);
                        }
                    }
                }
            }
        }



        #endregion

    }

    public static class VariablesExtensions
    {
        private static Regex _variablesRegex = new Regex(@"(?<=\$)(\w+)", RegexOptions.Compiled);
        private static string _variablePatternFormat = @"\B\${0}\b";

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

        public static string ReplaceVariables(this string input, IDictionary<string, string> variableValues)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                throw new ArgumentNullException("input");
            }

            if (variableValues == null)
            {
                throw new ArgumentNullException("variableValues");
            }

            var variables = input.GetVariables();

            foreach (var variable in variables)
            {
                string variableValue;

                if (!variableValues.TryGetValue(variable, out variableValue))
                {
                    throw new ArgumentException(string.Format("The variable '{0}' is not present", variable));
                }

                if (string.IsNullOrWhiteSpace(variableValue))
                {
                    throw new ArgumentException(string.Format("The value of the variable '{0}' is empty", variable));
                }

                var variableRegex = new Regex(string.Format(_variablePatternFormat, variable));
                input = variableRegex.Replace(input, variableValue);                
            }

            return input;
        }
    }
}
