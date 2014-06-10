using GalaSoft.MvvmLight;
using Lime.Protocol.Network;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Client.TestConsole.ViewModels
{
    public class EnvelopeViewModel : ViewModelBase
    {

        private string _json;

        public string Json
        {
            get { return _json; }
            set 
            {
                try
                {
                    var json = JObject.Parse(value);
                    _json = json.ToString(Formatting.Indented);
                }
                catch
                {

                    _json = value;    
                }
                
                RaisePropertyChanged(() => Json);
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


    }
}
