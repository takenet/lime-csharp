using GalaSoft.MvvmLight;
using Lime.Client.TestConsole.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Client.TestConsole.ViewModels
{
    public class SessionViewModel : ViewModelBase
    {

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



        public AsyncCommand OpenTransportCommand { get; private set; }

        public AsyncCommand CloseTransportCommand { get; private set; }

    }
}
