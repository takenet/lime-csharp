using FirstFloor.ModernUI;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Lime.Client.Windows.ViewModels
{
    public class TraceViewModel : ViewModelBase, ITraceWriter
    {
        #region Constructor

        public TraceViewModel()
        {
            TraceLogs = new ObservableCollection<TraceDataViewModel>();
            TraceLimit = 100;

            LoadedCommand = new RelayCommand(Loaded);
            ClosingCommand = new RelayCommand(Closing);
            ClosedCommand = new RelayCommand(Closed);
        }

        #endregion

        private int _traceLimit;
        public int TraceLimit
        {
            get { return _traceLimit; }
            set
            {
                _traceLimit = value;
                RaisePropertyChanged(() => TraceLimit);
            }
        }

        private string _filter;
        public string Filter
        {
            get { return _filter; }
            set
            {
                _filter = value;
                RaisePropertyChanged(() => Filter);
                TraceLogsView.Refresh();
            }
        }

        private int _dataLenght;
        public int DataLenght
        {
            get { return _dataLenght; }
            set
            {
                _dataLenght = value;
                RaisePropertyChanged(() => DataLenght);
            }
        }

        private ObservableCollection<TraceDataViewModel> _traceLogs;
        public ObservableCollection<TraceDataViewModel> TraceLogs
        {
            get { return _traceLogs; }
            set
            {
                _traceLogs = value;

                if (_traceLogs != null)
                {
                    TraceLogsView = CollectionViewSource.GetDefaultView(_traceLogs);

                    if (!ModernUIHelper.IsInDesignMode)
                    {
                        TraceLogsView.Filter = new Predicate<object>(o =>
                        {
                            var traceDataViewModel = o as TraceDataViewModel;

                            if (traceDataViewModel != null &&
                                traceDataViewModel.Data != null &&
                                !string.IsNullOrWhiteSpace(_filter))
                            {
                                return CultureInfo.CurrentCulture.CompareInfo.IndexOf(
                                    traceDataViewModel.Data,
                                    _filter,
                                    CompareOptions.IgnoreCase) >= 0;
                            }

                            return true;
                        });
                    }
                }
            }
        }

        public ICollectionView TraceLogsView { get; private set; }

        #region ITraceWriter Members

        public Task TraceAsync(string data, DataOperation operation)
        {
            return App.Current.Dispatcher.InvokeAsync(() =>
            {
                if (!string.IsNullOrWhiteSpace(data))
                {
                    DataLenght += data.Length;

                    var json = JObject.Parse(data);
                    json.ToString(Formatting.Indented);
                    var traceData = new TraceDataViewModel()
                    {
                        Operation = operation,
                        Data = json.ToString()
                    };

                    TraceLogs.Add(traceData);

                    while (TraceLogs.Count > TraceLimit)
                    {
                        TraceLogs.RemoveAt(0);
                    }
                }
            }).Task;
        }

        private bool _isEnabled;
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set
            {
                _isEnabled = value;
                RaisePropertyChanged(() => IsEnabled);
            }
        }


        #endregion


        #region Window Commands

        public RelayCommand LoadedCommand { get; private set; }

        public void Loaded()
        {
            IsEnabled = true;
        }

        public RelayCommand ClosingCommand { get; private set; }

        /// <summary>
        /// The window is closing
        /// </summary>
        private void Closing()
        {
            IsEnabled = false;
        }

        public RelayCommand ClosedCommand { get; private set; }

        /// <summary>
        /// The window is closed
        /// </summary>
        private void Closed()
        {
        }

        #endregion
    }

    public class TraceDataViewModel : ViewModelBase
    {
        private DataOperation _operation;
        public DataOperation Operation
        {
            get { return _operation; }
            set
            {
                _operation = value;
                RaisePropertyChanged(() => Operation);
            }
        }

        private string _data;
        public string Data
        {
            get { return _data; }
            set
            {
                _data = value;
                RaisePropertyChanged(() => Data);
            }
        }
    }
}
