using Desktop.Model;
using Desktop.Model.Desktop;
using Desktop.Services.Interfaces;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Events;
using Prism.Services.Dialogs;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.Grid.Converter;
using Syncfusion.XlsIO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Desktop.Dialogs
{

    public sealed class VisitorsDialogViewModel : DialogBase
    {
        internal const string TIME_SPAN = "TIME";
        public const string TITLE = "T";

        private SimpleRequest _selectedItem;


        public VisitorsDialogViewModel(IDialogService dialogService, IDataService dataService, IEventAggregator eventAggregator)
            : base(dialogService, dataService, eventAggregator)
        {


            FireWallAction = new DelegateCommand<SimpleRequest>(PerformFireWallAction, IsSelected);
            Tracert = new DelegateCommand<SimpleRequest>(PerformTracert, IsSelected);
            NavigateToUrl = new DelegateCommand<CurrentCellRequestNavigateEventArgs>(PerformNavigation);
            OpenCounty = new DelegateCommand<object>(SetSelectedCountry);

        }




        public DelegateCommand<SimpleRequest> Tracert { get; private set; }
        private void PerformNavigation(CurrentCellRequestNavigateEventArgs obj)
        {

        }

        public DelegateCommand<CurrentCellRequestNavigateEventArgs> NavigateToUrl { get; private set; }
        private bool IsSelected(object arg)
        {
            return SelectedItem is not null;
        }

        private void PerformTracert(SimpleRequest obj)
        {

            var p = new DialogParameters
            {
                { TracertViewModel.IPADDRESS, obj.IPAddress },
                { TracertViewModel.USER, obj.UserType },

            };
            DialogService.ShowDialog(DialogNames.TRACERT_DIALOGUE, parameters: p, callback);
            void callback(IDialogResult obj)
            {

            }
        }



        TimeSpan _timeStamp;
        private ObservableCollection<MapRequest> _mapData = new();
        private MapRequest _mapSelected;
        private DataGranullarityFilter _dataGranullarity;

        public DelegateCommand<object> OpenCounty { get; private set; }
        public DelegateCommand<SimpleRequest> FireWallAction { get; private set; }
        public override void OnDialogOpened(IDialogParameters parameters)
        {
            LoadingProgress = "Please wait...";
            Title = parameters.GetValue<string>(TITLE);
            _timeStamp = parameters.GetValue<TimeSpan>(TIME_SPAN);
            IsBusy = true;
            LoadingProgress = "Request map data";
            DataService.GetMapRequests(_timeStamp, PageTokenSource.Token).ContinueWith(PopulateChartData);



        }
        public System.Windows.Point MapCenter { get => _mapCenter; set => SetProperty(ref _mapCenter , value); }
        public ObservableCollection<MapRequest> MapData { get => _mapData; set => SetProperty(ref _mapData, value); }
        public MapRequest MapSelected
        {
            get => _mapSelected;
            set => SetProperty(ref _mapSelected, value);
        }


        List<MapRequest> _source;
        private void PopulateChartData(Task<List<MapRequest>> task)
        {
            if (InvokeRequired())
            {
                InvokeIfNecessary(() => PopulateChartData(task));
                return;
            }
            LoadingProgress = "Populate chart...";
            try
            {
                if (task.Result is List<MapRequest> list)
                {
                    _source = list;

                    if (list.Count > 0)
                    {
                        var selectedMap = list.OrderByDescending(o => o.Requests).First();
                        MapCenter = new System.Windows.Point(selectedMap.LocationMap.Lat, selectedMap.LocationMap.Lng);

                        MapData.AddRange(list);


                        SetSelectedCountry(selectedMap);
                    }
                }
            }
            finally
            {
                IsBusy = false;

            }

        }
        public DataGranullarityFilter DataGranullarity { get => _dataGranullarity; set => SetProperty(ref _dataGranullarity, value); }


        private async void SetSelectedCountry(object sender)
        {
            if (sender is MapRequest request)
            {

                IsBusy = true;
                LoadingProgress = $"Get request data for {request}...";

                MapSelected = request;
                Visitors.Clear();

                var items = DataGranullarity switch
                {
                    DataGranullarityFilter.IPAddress => await DataService.GetSimpleRequestIPAddress(timeStamp: _timeStamp, cidr: request.CIDR, cancelationToken: PageTokenSource.Token),
                    DataGranullarityFilter.ISP => await DataService.GetSimpleRequestISP(timeStamp: _timeStamp, cidr: request.CIDR, cancelationToken: PageTokenSource.Token),
                    DataGranullarityFilter.Country => await DataService.GetSimpleRequestCountry(timeStamp: _timeStamp, country: request.Location, cancelationToken: PageTokenSource.Token),
                    _ => await DataService.GetSimpleRequestCountry(timeStamp: _timeStamp, country: request.Location, cancelationToken: PageTokenSource.Token),

                };
                if (items is not null && items.Count > 0)
                {
                    LoadingProgress = $"Populate grid with {items.Count:N0} rows...";
                    Visitors.AddRange(items);
                    SelectedItem = items[0];
                }


                ExportToExcel?.RaiseCanExecuteChanged();
            }

            IsBusy = false;

        }

        public ObservableCollection<SimpleRequest> Visitors { get; set; } = new();

        public SimpleRequest SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value))
                {
                    FireWallAction?.RaiseCanExecuteChanged();
                    Tracert.RaiseCanExecuteChanged();
                }
            }
        }

        private void PerformFireWallAction(SimpleRequest obj)
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

        private bool _showHumans = true;

        public bool ShowHumans
        {
            get => _showHumans;
            set
            {

                if (value)
                {
                    MapData.AddRange(_source.Where(w => w.IsHuman));
                }
                else
                {
                    for (int i = MapData.Count - 1; i >= 0; i--)
                    {
                        if (MapData[i].IsHuman)
                        {
                            MapData.RemoveAt(i);
                        }
                    }
                }
                SetProperty(ref _showHumans, value);
                var selectedMap = MapData.OrderByDescending(o => o.Requests).First();
                MapCenter = new System.Windows.Point(selectedMap.LocationMap.Lat, selectedMap.LocationMap.Lng);

            }
        }

        private bool _showBots = true;

        public bool ShowBots
        {
            get => _showBots;
            set
            {

                if (value)
                {
                    MapData.AddRange(_source.Where(w => w.IsBot));
                }
                else
                {
                    for (int i = MapData.Count - 1; i >= 0; i--)
                    {
                        if (MapData[i].IsHuman)
                        {
                            MapData.RemoveAt(i);
                        }
                    }
                }
                SetProperty(ref _showBots, value);
                var selectedMap = MapData.OrderByDescending(o => o.Requests).First();
                MapCenter = new System.Windows.Point(selectedMap.LocationMap.Lat, selectedMap.LocationMap.Lng);

            }
        }
        private bool _showSearchEngines = true;
        private Point _mapCenter;

        public bool ShowSearchEngines
        {
            get => _showSearchEngines;
            set
            {

                if (value)
                {
                    MapData.AddRange(_source.Where(w => w.IsSearchEngine));
                }
                else
                {
                    for (int i = MapData.Count - 1; i >= 0; i--)
                    {
                        if (MapData[i].IsHuman)
                        {
                            MapData.RemoveAt(i);
                        }
                    }
                }
                SetProperty(ref _showSearchEngines, value);
                var selectedMap = MapData.OrderByDescending(o => o.Requests).First();
                MapCenter = new System.Windows.Point(selectedMap.LocationMap.Lat, selectedMap.LocationMap.Lng);

            }
        }

    }
}
