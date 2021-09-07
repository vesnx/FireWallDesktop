using Desktop.Core.Dialogs;
using Desktop.Model;
using Desktop.Services.Interfaces;
using Prism.Commands;
using Prism.Events;
using Prism.Services.Dialogs;
using Syncfusion.UI.Xaml.Maps;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Walter.BOM.Geo;
using static Syncfusion.XlsIO.Parser.Biff_Records.Charts.ChartMarkerFormatRecord;

namespace Desktop.Dialogs
{
    public class TracertViewModel : DialogBase
    {
        public class TraceMapItem
        {

            private Walter.BOM.Geo.SimpleLatLong _latLong;
            public TraceMapItem(SimpleTracertEntry item)
            {

                _latLong = item.Location.GetCapitol();
                if (item.Lng != _latLong.Lng)
                {
                    _latLong = new Walter.BOM.Geo.SimpleLatLong() { City = item.City, Country = item.Country, Lat = item.Lat, Lng = item.Lng };
                }
                HopId = item.HopID;
                Label = string.Concat(item.HostName, " ", item.City, " ", item.Country).Trim();
                IPAddress = item.Address;
                HostName = item.HostName;
                Lng = _latLong.Lng;
                Lat = _latLong.Lat;
                Latitude = Lat.LatitudeDegrees();
                Longitude = Lng.LongitudeDegrees();
                
                
            }

            public int HopId { get; set; }
            public string Label { get; set; }
            public string IPAddress { get; set; }
            public string HostName { get; set; }
            public string Latitude { get; set; }
            public string Longitude { get; set; }
            public double Lng { get; set; }
            public double Lat { get; set; }
        }

        internal const string IPADDRESS = "IP";
        internal const string USER = "user";

        private readonly ObservableCollection<TraceMapItem> _traceMap = new();
        private readonly ObservableCollection<SimpleTracertEntry> _traceRout = new();
        private readonly SimpleTracertEntry _serverLocation;
        public TracertViewModel(IDialogService dialogService, IDataService dataService,IEventAggregator eventAggregator)
            :base(dialogService,dataService,eventAggregator)
        {


            ISPDetail = new DelegateCommand(PerformISPDetail);

            _serverLocation = new SimpleTracertEntry(RunTime.SelectedFireWall);
            var map = new TraceMapItem(_serverLocation );
            TraceMap.Add(map);
            TraceRout.Add(_serverLocation);

        }


        public DelegateCommand ISPDetail { get; private set; }

        protected async void PerformISPDetail()
        {
            IsBusy = true;
            try
            {
                if (System.Net.IPAddress.TryParse(SelectedItem.Address, out _))
                {
                    await base.ISPDetails(SelectedItem.Address);
     
                }
            }
            finally
            {
                IsBusy = false;
            }

        }

        public ObservableCollection<TraceMapItem> TraceMap { get => _traceMap; }
        public ObservableCollection<SimpleTracertEntry> TraceRout { get => _traceRout; }

        public SimpleTracertEntry SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value))
                {
                    ISPDetail.RaiseCanExecuteChanged();
                }
            }
        }



        public override void OnDialogOpened(IDialogParameters parameters)
        {
            var IP = parameters.GetValue<string>(IPADDRESS);
            ServerLocation = RunTime.SelectedFireWall.DisplayName;
            Desitination = IP;
            IsBusy = true;
            Title = $"Network trace from {ServerLocation} to {IP}";
            DataService.GetTraceRout(IP, PageTokenSource.Token).ContinueWith(LoadTraceData, PageTokenSource.Token);

        }



        private void LoadTraceData(Task<List<SimpleTracertEntry>> task)
        {
            if (InvokeRequired())
            {
                Invoke(() => LoadTraceData(task));
                return;
            }
            try
            {
                if (task.Exception is not null)
                {
                    DialogService.ShowMessageDialog(message: "Calling the trace rout for the entry failed", "Method failed", icon: System.Windows.MessageBoxImage.Error);
                    return;
                }
                if (task.Result is null)
                {
                    DialogService.ShowMessageDialog(message: "Calling the trace rout returned no data", "Method failed", icon: System.Windows.MessageBoxImage.Error);
                    return;
                }


                TraceRout.AddRange(task.Result);




                for (int i = 0; i < task.Result.Count; i++)
                {
                    var candidate = task.Result[i];
                    if (TraceMap.Any(a => string.Equals(a.IPAddress, candidate.Address)))
                    {                        
                        continue;
                    }

                    TraceMap.Add(new(candidate));
                }


            
            }
            finally
            { 
                IsBusy = false;
            }
        }

        private string serverLocation;
        private string _desitination;
        private string _user;

        public string ServerLocation { get => serverLocation; set => SetProperty(ref serverLocation, value); }
        public string Desitination { get => _desitination; set => SetProperty(ref _desitination, value); }
        public string User { get => _user; set => SetProperty(ref _user, value); }


        private SimpleTracertEntry _selectedItem;


       



    }
}
