using Desktop.Model;
using Desktop.Model.Desktop;
using Desktop.Services.Interfaces;
using log4net;
using Prism.Commands;
using Prism.Events;
using Prism.Services.Dialogs;
using Syncfusion.UI.Xaml.Grid;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Walter.BOM.Geo;

namespace Desktop.Dialogs
{

    public class CountryDialogViewModel : DialogBase
    {
        internal const string COUNTRY = "COUNTRY";
        internal const string DATA = "DATA";
        internal const string SCOPE = "SCOPE";
        private readonly ILog _log;
        private SimpleCountry _countryFacts;
        private GeoLocation _location;
        private System.Windows.Point _mapCenter;
        private SimpleVisitSession _selectedItem;
        private TimeSpan _timeSpan;
        private Visibility _requestDataVisibility = Visibility.Collapsed;
        private Visibility _IncidentDataVisibility = Visibility.Collapsed;
        private Visibility _gridDataVisibility = Visibility.Visible;

        public CountryDialogViewModel(IDialogService dialogService, IDataService service, IEventAggregator eventAggregator,  ILog log)
                    : base(dialogService, service, eventAggregator)
        {
            _log = log;
            _log.LogCreated();

            SetSelected = new DelegateCommand<object>(SetSelectedMapItem);
            FireWallAction = new DelegateCommand(PerformFireWallAction, IsSelected);
            Tracert = new DelegateCommand(PerformTracert, IsSelected);
            ViewDetails = new DelegateCommand(PerformViewDetails, IsSelected);

        }



        public enum DataTypes
        {
            Visitors = 1,
            Malisous = 2,
            ISP = 3,
        }
        public SimpleCountry CountryFacts { get => _countryFacts; private set => SetProperty(ref _countryFacts, value); }


        public Visibility GridDataVisibility { get => _gridDataVisibility; set =>SetProperty(ref _gridDataVisibility , value); }
        public Visibility RequestDataVisibility  { get => _requestDataVisibility; set => SetProperty(ref _requestDataVisibility, value); }
        public Visibility IncidentDataVisibility  { get => _IncidentDataVisibility; set => SetProperty(ref _IncidentDataVisibility, value); }
        public DelegateCommand FireWallAction { get; private set; }
        public ObservableCollection<object> GridData { get; set; } = new();
        public ObservableCollection<SimpleRequest> SimpleRequestData { get; set; } = new();
        public ObservableCollection<SimpleIncident> SimpleIncidentData { get; set; } = new();
        public System.Windows.Point MapCenter { get => _mapCenter; set => SetProperty(ref _mapCenter, value); }
        public ObservableCollection<LocationCount> MapData { get; set; } = new();
        public SimpleVisitSession SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value))
                {
                    FireWallAction.RaiseCanExecuteChanged();
                    Tracert.RaiseCanExecuteChanged();
                    ViewDetails.RaiseCanExecuteChanged();

                }
            }
        }

        public DelegateCommand<object> SetSelected { get; set; }
        public DelegateCommand Tracert { get; private set; }
        public DelegateCommand ViewDetails { get; private set; }
        public override void OnDialogOpened(IDialogParameters parameters)
        {
            _location = parameters.GetValue<GeoLocation>(COUNTRY);
            var latLong = _location.GetCapitol();
            var data = parameters.GetValue<DataTypes>(DATA);
            _timeSpan = parameters.GetValue<TimeSpan>(SCOPE);

            IsBusy = true;
            var country = _location.GetCountryName();
            MapCenter = new System.Windows.Point(latLong.Lat, latLong.Lng);
            LoadingProgress = $"Get {country} data...";





            DataService.GetSimpleCountry(_location, PageTokenSource.Token).ContinueWith(PopulateCountryGrid, PageTokenSource.Token);
            switch (data)
            {
                case DataTypes.Visitors:
                    Title = $"{country} visitors between {DateTime.Now.Subtract(_timeSpan)} and {DateTime.Now}";

                    DataService.GetSimpleRequestCountry(timeStamp: _timeSpan, country: _location, cancelationToken: PageTokenSource.Token)
                        .ContinueWith(Populate);
                    break;
                case DataTypes.Malisous:
                    Title = $"Malicious activity detections in {country} between {DateTime.Now.Subtract(_timeSpan)} and {DateTime.Now}";
                    DataService.GetSimpleIncidentsCountry(timeStamp: _timeSpan, country: _location, cancelationToken: PageTokenSource.Token)
                        .ContinueWith(Populate);

                    break;
                default:
                    throw new NotImplementedException($"TODO Country data implementation for {data}");
            }








        }

        private bool CanExportToExcel(object _)
        {
            return GridData.Count > 0;
        }




        private bool IsSelected()
        {
            return _selectedItem is not null;
        }

        private void OnLogWebRequest(string value)
        {
            LoadingProgress = value;

        }

        private void PerformFireWallAction()
        {
            var p = new DialogParameters
            {
                { QuickActionViewModel.OBJECT, SelectedItem },

            };
            DialogService.ShowDialog(DialogNames.QUICKACTION, parameters: p, callback);
            void callback(IDialogResult obj)
            {

            }
        }


        private void PerformTracert()
        {
            DialogParameters p = new DialogParameters{
                { TracertViewModel.IPADDRESS, SelectedItem.IPAddress }
             };

            DialogService.ShowDialog(DialogNames.TRACERT_DIALOGUE, parameters: p, callback);
            void callback(IDialogResult obj)
            {

            }
        }

        private void PerformViewDetails()
        {
            var p = new DialogParameters
            {
                { RequestFlowViewModel.OBJECT, SelectedItem },
                { RequestFlowViewModel.SCOPE, _timeSpan },

            };
            DialogService.ShowDialog(DialogNames.REQUESTFLOW, parameters: p, callback);
            void callback(IDialogResult obj)
            {

            }
        }
        private void Populate(Task<List<SimpleIncident>> task)
        {
            if (InvokeRequired())
            {
                Invoke(() => Populate(task));
                return;
            }

            LoadingProgress = $"Populating incident data";
            try
            {
                if (task.Result is List<SimpleIncident> incidents)
                {
                    Dictionary<long, SimpleVisitSession> users = new();
                    var list = new List<LocationCount>();
                    foreach (var item in incidents.GroupBy(g => g.ApproximateLocation.GetHashCode()))
                    {
                        list.Add(new LocationCount(_location
                                                   , item.Count()
                                                   , item.First().ApproximateLocation)
                        {
                            Tag = new List<SimpleIncident>(item.Select(s => s))
                        });
                    }

                    MapData.AddRange(list);

                    foreach (var item in incidents)
                    {
                        if (!users.TryGetValue(item.FWUID, out var simpleVisit))
                        {
                            simpleVisit = new SimpleVisitSession()
                            {
                                Location = item.Location,
                                IPAddress = item.IPAddress,
                                CIDR = item.CIDR,
                                FireWallUser = item.FWUID,
                                City = item.ApproximateLocation.City,
                            };
                            users[item.FWUID] = simpleVisit;
                        }

                        simpleVisit.Activity.Add(new(item.URL.AbsoluteUri, null, item.CreatedUTC) { Tag = item });
                    }

                    GridData.AddRange(users.Values);
                }
            }

            finally
            {
                IsBusy = false;
            }

        }
        private void Populate(Task<List<SimpleRequest>> task)
        {
            LoadingProgress = $"Populating request data";
            try
            {
                if (task.Result is List<SimpleRequest> requests)
                {
                    Dictionary<long, SimpleVisitSession> users = new Dictionary<long, SimpleVisitSession>();
                    var list = new List<LocationCount>();
                    foreach (var item in requests.GroupBy(g => g.ApproximateLocation.GetHashCode()))
                    {
                        list.Add(
                            new LocationCount(_location, item.Count(), item.First().ApproximateLocation) { Tag = new List<SimpleRequest>(item.Select(s => s)) }
                            );

                    }

                    InvokeIfNecessary(() => MapData.AddRange(list));

                    foreach (var item in requests)
                    {
                        if (!users.TryGetValue(item.FWUID, out var simpleVisit))
                        {
                            simpleVisit = new SimpleVisitSession()
                            {
                                Location = item.Location,
                                IPAddress = item.IPAddress,
                                CIDR = item.CIDR,
                                FireWallUser = item.FWUID,
                                City = item.MapLocation.City,

                            };
                            users[item.FWUID] = simpleVisit;
                        }

                        simpleVisit.Activity.Add(new(item.URL.AbsoluteUri, item.Referrer, item.CreatedUTC) { Tag = item });
                    }

                    InvokeIfNecessary(() => GridData.AddRange(users.Values));
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void PopulateCountryGrid(Task<SimpleCountry> task)
        {
            if (task.Result is SimpleCountry country)
            {
                InvokeIfNecessary(() => CountryFacts = country);
            }
        }



        private void SetSelectedMapItem(object obj)
        {
            if (InvokeRequired())
            {
                InvokeIfNecessary(() => SetSelectedMapItem(obj));
                return;
            }

            GridDataVisibility= Visibility.Collapsed;
            
            if (obj is List<SimpleRequest> data)
            {
                RequestDataVisibility = Visibility.Visible;
                SimpleRequestData.Clear();
                for (int i = 0; i < data.Count; i++)
                {
                    SimpleRequestData.Add(data[i]);
                }

            }
            else if (obj is List<SimpleIncident> data2)
            {
                IncidentDataVisibility = Visibility.Visible;
                SimpleIncidentData.Clear();
                for (int i = 0; i < data2.Count; i++)
                {
                    SimpleIncidentData.Add(data2[i]);
                }
            }


        }
    }
}
