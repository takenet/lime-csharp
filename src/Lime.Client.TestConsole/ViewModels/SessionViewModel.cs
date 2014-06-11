using GalaSoft.MvvmLight;
using Lime.Client.TestConsole.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Lime.Client.TestConsole.ViewModels
{
    public class SessionViewModel : ViewModelBase
    {
        #region Constructor

        public SessionViewModel()
        {
            this.AvailableTransports = new[] { "Tcp" };
            this.SelectedTransport = AvailableTransports.First();
            this.Envelopes = new ObservableCollectionEx<EnvelopeViewModel>();
            this.Variables = new ObservableCollectionEx<VariableViewModel>();
            this.Templates = new ObservableCollectionEx<TemplateViewModel>();
        }

        #endregion

        #region Data Properties

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
            }
        }

        private string _hostUri;

        public string HostUri
        {
            get { return _hostUri; }
            set
            {
                _hostUri = value;
                RaisePropertyChanged(() => HostUri);
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

        public AsyncCommand CloseTransportCommand { get; private set; }

        public AsyncCommand SendCommand { get; private set; }

        #endregion

    }
}
