using GalaSoft.MvvmLight;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lime.Protocol.Serialization.Newtonsoft;

namespace Lime.Client.TestConsole.ViewModels
{
    public class EnvelopeViewModel : ViewModelBase
    {
        private static IEnvelopeSerializer _serializer;
        private bool _shouldIndendJson;

        static EnvelopeViewModel()
        {
            _serializer = new JsonNetSerializer();
        }


        public EnvelopeViewModel()
            : this(true)
        {

        }

        public EnvelopeViewModel(bool shouldIndendJson)
        {
            _shouldIndendJson = shouldIndendJson;
        }        



        #region Data Properties


        public void IndentJson()
        {
            Json = Json.IndentJson();
        }

        
        private string _json;
        private bool _isSettingJson;

        public string Json
        {
            get  { return _json; }
            set
            {
                _isSettingJson = true;

                try
                {
                    if (_shouldIndendJson)
                    {

                        // Json indentation
                        try
                        {
                            _json = value.IndentJson();
                        }
                        catch
                        {
                            _json = value;
                        }
                    }
                    else
                    {
                        _json = value;
                    }

                    RaisePropertyChanged(() => Json);

                    // Updates the Envelope property
                    // if it is not the caller
                    if (!_isSettingEnvelope)
                    {
                        try
                        {
                            Envelope = _serializer.Deserialize(_json);
                        }
                        catch 
                        {
                            Envelope = null;
                        }
                    }
                }
                finally
                {
                    _isSettingJson = false;
                }
            }
        }

        private Envelope _envelope;
        private bool _isSettingEnvelope;

        public Envelope Envelope
        {
            get { return _envelope;  }
            set 
            {
                _isSettingEnvelope = true;

                try
                {
                    _envelope = value;
                    RaisePropertyChanged(() => Envelope);

                    // Updates the Json property
                    // if it is not the caller
                    if (!_isSettingJson)
                    {
                        try
                        {
                            Json = _serializer.Serialize(_envelope);
                        }
                        catch
                        {
                            Json = null;
                        }
                    }
                }
                finally
                {
                    _isSettingEnvelope = false;
                }
            }
        }

        private DataOperation _direction;

        public DataOperation Direction
        {
            get { return _direction; }
            set
            {
                _direction = value;
                RaisePropertyChanged(() => Direction);
            }
        }


        private bool _isRaw;

        /// <summary>
        /// Indicates if the data 
        /// was collected directly from
        /// the transport trace writer
        /// </summary>
        public bool IsRaw
        {
            get { return _isRaw; }
            set
            {
                _isRaw = value;
                RaisePropertyChanged(() => IsRaw);
            }
        }

        #endregion



    }
}
