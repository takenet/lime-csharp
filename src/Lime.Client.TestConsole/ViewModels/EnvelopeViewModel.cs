using CommunityToolkit.Mvvm.ComponentModel;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Lime.Protocol.Serialization.Newtonsoft;
using Newtonsoft.Json.Linq;
using System;

namespace Lime.Client.TestConsole.ViewModels
{
    public class EnvelopeViewModel : ObservableRecipient
    {
        private static IEnvelopeSerializer _serializer;
        private readonly bool _shouldIndendJson;

        static EnvelopeViewModel()
        {
            _serializer = new EnvelopeSerializer(new DocumentTypeResolver());
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
            get { return _json; }
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

                    OnPropertyChanged(nameof(Json));

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
            get { return _envelope; }
            set
            {
                _isSettingEnvelope = true;

                try
                {
                    _envelope = value;
                    OnPropertyChanged(nameof(Envelope));

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
                OnPropertyChanged(nameof(Direction));
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
                OnPropertyChanged(nameof(IsRaw));
            }
        }

        #endregion Data Properties

        public static EnvelopeViewModel Parse(string inputJson)
        {
            try
            {
                var jsonObject = JObject.Parse(inputJson);

                if (jsonObject.HasValues)
                {
                    var envelopeViewModel = new EnvelopeViewModel
                    {
                        Json = inputJson
                    };

                    if (envelopeViewModel.Envelope != null)
                    {
                        return envelopeViewModel;
                    }
                    else
                    {
                        throw new ArgumentException("The input is a valid JSON document, but is not an Envelope");
                    }
                }
                else
                {
                    throw new ArgumentException("The input is a invalid or empty JSON document");
                }
            }
            catch (Exception e)
            when (!(e is ArgumentException))
            {
                throw new ArgumentException("The input is a invalid JSON document", e);
            }
        }

        public static bool TryParse(string inputJson, out EnvelopeViewModel envelopeViewModel)
        {
            try
            {
                envelopeViewModel = Parse(inputJson);
                return true;
            }
            catch
            {
                envelopeViewModel = null;
                return false;
            }
        }
    }
}