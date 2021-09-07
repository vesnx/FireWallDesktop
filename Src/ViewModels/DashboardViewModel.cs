using Desktop.Core.Dialogs;
using Desktop.Dialogs;
using Desktop.Infrastructure;
using Desktop.Model;
using Desktop.Model.Desktop;
using Desktop.Model.Events;
using Desktop.Model.Infrastructure;
using Desktop.Services.Interfaces;
using log4net;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Regions;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Walter.BOM;
using Walter.BOM.Geo;

namespace Desktop.ViewModels
{




    public class DashboardViewModel : ConfigurableViewModel, INavigationAware
    {
        private readonly UserPrefference.Desktop _desktop;
        private readonly IEventAggregator _eventAggregator;
        private readonly IRegionManager _regionManager;
        private readonly IContainerProvider _containerProvider;
        private readonly ILog _log;
        private readonly IDataService _service;
        private int _blockedUsers;
        private int _maliciousUserAgentsCount;
        private int _newUsers;
        private int _portBasedAttacks;
        private int _visitsBySearchEnginesCount;
        private int incidents;
        private Walter.BOM.LicenseStatus licenseStatus;
        private TimeSpan upTime;
        private System.Timers.Timer _timer = new System.Timers.Timer(1000);
        CancellationTokenSource PageTokenSource = new();
        private int visitors;
        private DateTime _serverTime;
        private MapLocation _serverLocation;
        private TimeZoneInfo _serverTimeZone;
        private readonly FireWallRequestsLogEvent _event;

        public DashboardViewModel(UserPrefference userPrefference, IDataService service
            , IEventAggregator eventAggregator
            , IRegionManager regionManager, IContainerProvider containerProvider, ILog log)
: base(userPrefference.DesktopProperties, regionManager)
        {
            _event = eventAggregator.GetEvent<FireWallRequestsLogEvent>();
            _timer = new System.Timers.Timer(AutoRefreshInterval.TotalMilliseconds);
            _timer.Elapsed += RefreshHeader;
            _timer.AutoReset = true;
            _desktop = userPrefference.DesktopProperties;
            _service = service;
            _eventAggregator = eventAggregator;
            _regionManager = regionManager;
            _containerProvider = containerProvider;
            _log = log;
            NavigateToMaliciousUserAgents = new DelegateCommand(OnNavigateViaAdminPanelToMaliciousUserAgents);
            ExpandVisitors = new DelegateCommand(PopUpVisitory, CanPopUpVisitory);//.ObservesCanExecute(() => Visitors > 0);
            ExpandIncidents = new DelegateCommand(PopUpIncidents, CanPopUpIncidents);//.ObservesCanExecute(() => Visitors > 0);
            FireWallAction = new DelegateCommand<SimpleISP>(PerformFireWallAction, IsISPSelected);
            ISPDetail = new DelegateCommand<SimpleISP>(PerformISPDetail, IsISPSelected);
            RefreshNow = new DelegateCommand(PerformRefresh, RefreshNowEnabled);
            OpenMaliciousCounty = new DelegateCommand<object>(PopUpMaliciouseCountries);
            OpenCounty = new DelegateCommand<object>(PopUpVisitorCountries);
            OpenPdf = new DelegateCommand<string>(PopUpOpenPdf);
            ServerLocation = RunTime.SelectedFireWall.Location;
            ServerCountry = ServerLocation?.Country ?? "";
            _ = _eventAggregator.GetEvent<FireWallStateChangedEvent>()
                     .Subscribe(
                          action: OnDisconnected
                         , threadOption: ThreadOption.PublisherThread
                         , keepSubscriberReferenceAlive: false
                         , filter => filter.NewState.HasFlag(FireWallStates.Disconnected) && filter.OldState.HasFlag(FireWallStates.IsConnected)
                         );

            _ = _eventAggregator.GetEvent<FireWallStateChangedEvent>()
                     .Subscribe(
                          action: OnReconnect
                         , threadOption: ThreadOption.BackgroundThread
                         , keepSubscriberReferenceAlive: false
                         , filter => filter.NewState.HasFlag(FireWallStates.IsConnected) && filter.OldState.HasFlag(FireWallStates.Disconnected)
                         );

            _desktop.PropertyChanged += _desktop_PropertyChanged;
            _log.LogCreated();
            
        }

        public DelegateCommand<string> OpenPdf { get; }


        private void PopUpOpenPdf(string name)
        {


            var file = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "Pdf", $"{name}.pdf"));
            var dlgService = _containerProvider.Resolve<IDialogService>();


            if (!file.Exists)
            {

                dlgService.ShowMessageDialog($"Could find the {name} report, please update the software", "File not found");
                return;
            }



            var p = new DialogParameters()
                {
                    { PdfViewerViewModel.PDF,file}
                };


            dlgService.ShowDialog(DialogNames.PDF_VIEWER, p, callback);
            void callback(IDialogResult obj)
            {
            }

        }

        private void PopUpVisitorCountries(object location)
        {
            var country = (GeoLocation)location;
            var dlgService = _containerProvider.Resolve<IDialogService>();
            var p = new DialogParameters
            {
                { CountryDialogViewModel.COUNTRY,country },
                { CountryDialogViewModel.DATA, CountryDialogViewModel.DataTypes.Visitors },
                { CountryDialogViewModel.SCOPE, _desktop.InitializeTimeRange },
            };

            dlgService.ShowDialog(DialogNames.COUNTRY_DIALOG, parameters: p, callback);



            void callback(IDialogResult obj)
            {

            }

        }

        private DelegateCommand<SimpleISP> generateAbuseReport;
        public DelegateCommand<SimpleISP> GenerateAbuseReport => generateAbuseReport ??= new DelegateCommand<SimpleISP>(PerformGenerateAbuseReport, CanGenerateAbuseReport);

        private bool CanGenerateAbuseReport(SimpleISP arg)
        {
            return arg is not null;
        }

        private void PerformGenerateAbuseReport(SimpleISP simple)
        {
            var dlgService = _containerProvider.Resolve<IDialogService>();
            var cmd = new AbuseReportCommand()
            {
                DataSource = AbuseReportCommand.Filter.ISPRangeId,
                Value = simple.Id,
                BreadCrumbs = false,
                PortBasedAttacks = true,
                WebRequests = true,
                From = DateTime.UtcNow.AddMonths(-1),
                Till = DateTime.UtcNow
            };

            var p = new DialogParameters()
            {
                { AbuseDateRangePickerViewModel.Command,cmd}
            };
            bool exit = true;

            dlgService.ShowDialog(DialogNames.ABUSE_REPORT_DATE_PICKER, parameters: p, (result) =>
            {
                exit = result.Result != ButtonResult.OK;

                if (exit == false && result.Parameters.TryGetValue<AbuseReportCommand>(AbuseDateRangePickerViewModel.Command, out var cmdClone))
                {
                    cmd.From = cmdClone.From.ToUniversalTime();
                    cmd.Till = cmdClone.Till.ToUniversalTime();
                    cmd.WebRequests = cmdClone.WebRequests;
                    cmd.PortBasedAttacks = cmdClone.PortBasedAttacks;
                }

            });


            if (exit)
            {
                return;
            }

            p = new DialogParameters()
            {
                { AbuseReportBuilderViewModel.COMMAND,cmd}
            };

            dlgService.ShowDialog(DialogNames.ABUSE_REPORT, parameters: p, callback);
            void callback(IDialogResult obj)
            {

            }

        }
        private void PopUpMaliciouseCountries(object location)
        {

            var country = (GeoLocation)location;
            var dlgService = _containerProvider.Resolve<IDialogService>();
            var p = new DialogParameters
            {

                { CountryDialogViewModel.COUNTRY,country },
                { CountryDialogViewModel.DATA, CountryDialogViewModel.DataTypes.Malisous },
                { CountryDialogViewModel.SCOPE, _desktop.InitializeTimeRange },

            };
            dlgService.ShowDialog(DialogNames.COUNTRY_DIALOG, parameters: p, callback);



            void callback(IDialogResult obj)
            {

            }
        }

        public DelegateCommand<object> OpenMaliciousCounty { get; private set; }
        public DelegateCommand<object> OpenCounty { get; private set; }

        private bool RefreshNowEnabled()
        {
            return !IsLoading;
        }

        private async void PerformRefresh()
        {
            SetBussyMessage("Loading..");
            try
            {
                if (PageTokenSource.IsCancellationRequested)
                {
                    PageTokenSource.Dispose();
                    PageTokenSource = new CancellationTokenSource();
                }
                var data = await _service.GetInitialized(_desktop.InitializeTimeRange, externalToken: PageTokenSource.Token);
                // .ContinueWith(Populate, PageTokenSource.Token).ConfigureAwait(false);

                Populate(data);

            }
            finally
            {
                IsLoading = false;
            }

        }
        public bool AutoRefresh
        {
            get => _autoRefresh;
            set
            {
                SetProperty(ref _autoRefresh, value);

                _timer.Enabled = value;
                if (_timer.Enabled)
                {
                    RefreshHeader(this, null);
                    _timer.Start();
                }
                else
                {
                    _timer.Stop();
                }
            }
        }
        public TimeSpan AutoRefreshInterval
        {
            get => _autoRefreshInterval;
            set
            {
                if (SetProperty(ref _autoRefreshInterval, value))
                {
                    _timer.Interval = value.TotalMilliseconds;
                }
            }
        }
        public DelegateCommand RefreshNow { get; private set; }

        private void _desktop_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "TimeScale":
                    RaisePropertyChanged(nameof(this.Scale));
                    RaisePropertyChanged(nameof(this.TimeTicks));
                    RaisePropertyChanged(nameof(this.DateRange));
                    SetBussyMessage("Updating...");
                    _service.GetInitialized(_desktop.InitializeTimeRange, externalToken: PageTokenSource.Token)
                            .ContinueWith(Populate, PageTokenSource.Token).ConfigureAwait(false);
                    break;
            }
        }

        private void PerformISPDetail(SimpleISP isp)
        {
            var dlgService = _containerProvider.Resolve<IDialogService>();
            var p = new DialogParameters
            {
                { WHOISDialogViewModel.ISP, isp },
                { WHOISDialogViewModel.TITLE, $"{isp.Organization} Details" }
            };
            dlgService.ShowDialog(DialogNames.ISPDETAIL_DIALOGUE, parameters: p, callback);
            void callback(IDialogResult obj)
            {

            }
        }

        private void PopUpIncidents()
        {
            var dlgService = _containerProvider.Resolve<IDialogService>();
            var p = new DialogParameters
            {

                {  IncedentsDialogViewModel.TITLE, $"{RunTime.SelectedFireWall.DisplayName} Incidents since {DateTime.Now.Subtract(_desktop.InitializeTimeRange)}" },
                {  IncedentsDialogViewModel.TIME_SPAN, _desktop.InitializeTimeRange }
            };
            dlgService.ShowDialog(DialogNames.IncidentStats_DIALOGUE, parameters: p, callback);
            void callback(IDialogResult obj)
            {

            }
        }

        private bool CanPopUpIncidents()
        {
            return LastIncident is not null;
        }

        private bool CanPopUpVisitory()
        {
            return LastRequest is not null;
        }

        private void PopUpVisitory()
        {
            var dlgService = _containerProvider.Resolve<IDialogService>();
            var p = new DialogParameters
            {
                { VisitorsDialogViewModel.TIME_SPAN, _desktop.InitializeTimeRange },
                { VisitorsDialogViewModel.TITLE, $"{RunTime.SelectedFireWall.DisplayName} visitors since {DateTime.Now.Subtract(_desktop.InitializeTimeRange)}" }
            };
            dlgService.ShowDialog(DialogNames.VisitStats_DIALOGUE, parameters: p, callback);



            void callback(IDialogResult obj)
            {

            }
        }

        private void OnReconnect(FireWallStateChangedArgs obj)
        {
            try
            {


                if (AutoRefresh)
                {
                    _timer.Start();
                }

                _service.GetInitialized(_desktop.InitializeTimeRange, externalToken: PageTokenSource.Token)
                    .ContinueWith(Populate, PageTokenSource.Token).ConfigureAwait(false);
            }
            catch { }
        }

        private void OnDisconnected(FireWallStateChangedArgs obj)
        {
            try
            {
                if (obj.NewState.HasFlag(FireWallStates.Disconnected))
                {
                    _timer?.Stop();
                    AutoRefresh = false;
                }
            }
            catch
            { }
        }

        bool _isLoadingHeader;
        private async void RefreshHeader(object sender, ElapsedEventArgs e)
        {
            if (_isLoadingHeader)
                return;

            if (RunTime.SelectedFireWall is null)
                return;

            _isLoadingHeader = true;

            var data = await _service.GetStatusHeader(this.PageTokenSource.Token);
            PopulateHeader(data);



            if (e is null)
            {
                var counters = await _service.GetStatusCounters(_desktop.InitializeTimeRange, PageTokenSource.Token);
                PopulateStatsCounters(counters);

                var locations = await _service.GetStatusVisitors(_desktop.InitializeTimeRange, PageTokenSource.Token);
                PopulateMalicousUsersChart(locations);

                locations = await _service.GetStatusMalicious(_desktop.InitializeTimeRange, PageTokenSource.Token);
                PopulateMalicousUsersChart(locations);

                var isp = await _service.GetStatusISP(_desktop.InitializeTimeRange, externalToken: PageTokenSource.Token);
                PopulateISPGrid(isp);

            }


        }



        public int BlockedUsers { get => _blockedUsers; private set => SetProperty(ref _blockedUsers, value); }



        public int Incidents { get => incidents; set => SetProperty(ref incidents, value); }
        public DateTime ServerTime { get => _serverTime; private set => SetProperty(ref _serverTime, value); }
        public MapLocation ServerLocation { get => _serverLocation; private set => SetProperty(ref _serverLocation, value); }
        public TimeZoneInfo ServerTimeZone { get => _serverTimeZone; private set => SetProperty(ref _serverTimeZone, value); }
        public Walter.BOM.LicenseStatus LicenseStatus { get => licenseStatus; set => SetProperty(ref licenseStatus, value); }

        public int MaliciousUserAgentsCount { get => _maliciousUserAgentsCount; set => SetProperty(ref _maliciousUserAgentsCount, value); }

        public DelegateCommand NavigateToMaliciousUserAgents { get; private set; }
        public DelegateCommand ExpandVisitors { get; private set; }
        public DelegateCommand ExpandIncidents { get; private set; }
        public int NewUsers { get => _newUsers; private set => SetProperty(ref _newUsers, value); }

        public int PortBasedAttacks { get => _portBasedAttacks; private set => SetProperty(ref _portBasedAttacks, value); }

        public UserPrefference.Desktop Settings
        {
            get => _desktop;
        }

        public TimeSpan UpTime { get => upTime; set => SetProperty(ref upTime, value); }

        public int Visitors { get => visitors; set => SetProperty(ref visitors, value); }
        public bool ChangeGrouping { get => _changeGrouping; set => SetProperty(ref _changeGrouping, value); }
        public ObservableCollection<LocationCount> VisitorsMap { get; } = new();

        public ObservableCollection<LocationCount> AttackersMap { get; } = new();
        public ObservableCollection<SimpleISP> ServiceProviders { get; } = new();


        public int VisitsBySearchEnginesCount { get => _visitsBySearchEnginesCount; set => SetProperty(ref _visitsBySearchEnginesCount, value); }


        private void OnNavigateViaAdminPanelToMaliciousUserAgents()
        {
            var parameters = new NavigationParameters();
            parameters.Add(ViewModels.MaliciousUserAgentsViewModel.TimeTicsParameter, _desktop.InitializeTimeRange);

            base.PerformNavigation(nameof(Views.MaliciousUserAgents), parameters);
        }
        private void PopulateHeader(Task<StatusHeader> task)
        {
            if (task.Exception is not null)
            {
                LogException(task.Exception);
                return;
            }
            if (task.IsCanceled)
                return;

            if (task.Result is StatusHeader data)
            {

                PopulateHeader(data);
            }
        }
        private async void PopulateHeader(StatusHeader data)
        {
            if (InvokeRequired())
            {
                InvokeIfNecessary(() => PopulateHeader(data));
                return;
            }
            _isLoadingHeader = true;
            if (this.Visitors != data.Visitors)
            {
                try
                {
                    var vs = await _service.GetStatusCounters(_desktop.InitializeTimeRange, PageTokenSource.Token);
                    PopulateStatsCounters(vs);
                }
                catch (Exception e)
                {
                    LogException(e);
                }
            }

            if (PageTokenSource.IsCancellationRequested)
                return;

            if (this.Incidents != data.Attacks)
            {
                try
                {
                    var d1 = await _service.GetStatusMalicious(_desktop.InitializeTimeRange, PageTokenSource.Token);
                    PopulateMalicousUsersChart(d1);
                    if (PageTokenSource.IsCancellationRequested)
                        return;

                    LastIncident = await _service.GetLastIncident(PageTokenSource.Token);
                    LastRequest = await _service.GetLastRequest(PageTokenSource.Token);

                    d1 = await _service.GetStatusVisitors(_desktop.InitializeTimeRange, PageTokenSource.Token);
                    PopulateVisitorsChart(d1);

                    if (PageTokenSource.IsCancellationRequested)
                        return;

                    var d2 = await _service.GetStatusISP(_desktop.InitializeTimeRange, externalToken: PageTokenSource.Token);
                    PopulateISPGrid(d2);
                }
                catch (Exception e)
                {
                    LogException(e);
                }
            }

            if (PageTokenSource.IsCancellationRequested)
                return;


            this.LicenseStatus = data.LicenseStatus;
            this.UpTime = data.UpTime;
            this.Visitors = data.Visitors;
            this.Incidents = data.Attacks;
            this.ServerTime = data.ServerDate;



            _isLoadingHeader = false;
        }

        bool updatingISP;
        private void PopulateISPGrid(Task<List<SimpleISP>> task)
        {
            if (task.Exception is not null)
            {
                LogException(task.Exception);
                return;
            }

            if (task.Result is IList<SimpleISP> data)
            {
                PopulateISPGrid(data);
            }
        }

        private void PopulateISPGrid(IList<SimpleISP> data)
        {

            if (updatingISP)
                return;

            SetBussyMessage("Update ISP Grid..");
            updatingISP = true;
            try
            {

                if (ServiceProviders.Count == 0)
                {
                    if (data.Count > 0)
                    {
                        InvokeIfNecessary(() => ServiceProviders.AddRange(data));

                    }
                }
                else
                {


                    for (int x = ServiceProviders.Count - 1; x >= 0; x--)
                    {
                        bool remove = true;
                        for (int i = data.Count - 1; i >= 0; i--)
                        {
                            if (ServiceProviders[x].Id == data[i].Id)
                            {
                                remove = false;
                                if (ServiceProviders[x].CountsChanged(data[i]))
                                {
                                    InvokeIfNecessary(() =>
                                    {
                                        ServiceProviders[x].Incidents = data[i].Incidents;
                                        ServiceProviders[x].IncidentsInTimeRange = data[i].IncidentsInTimeRange;
                                        ServiceProviders[x].IPAddressInTimeRange = data[i].IPAddressInTimeRange;
                                        ServiceProviders[x].LastIncident = data[i].LastIncident;
                                        ServiceProviders[x].LastPortsAttack = data[i].LastPortsAttack;
                                        ServiceProviders[x].LastSeen = data[i].LastSeen;
                                        ServiceProviders[x].LastTalkingDetection = data[i].LastTalkingDetection;
                                        ServiceProviders[x].PortBasedIncidents = data[i].PortBasedIncidents;
                                        ServiceProviders[x].PortsAttacked = data[i].PortsAttacked;
                                        ServiceProviders[x].TalkingAddresses = data[i].TalkingAddresses;
                                        ServiceProviders[x].TalkingApplications = data[i].TalkingApplications;
                                        ServiceProviders[x].TalkingDetections = data[i].TalkingDetections;
                                        ServiceProviders[x].Users = data[i].Users;
                                        ServiceProviders[x].UsersWithBlockStatus = data[i].UsersWithBlockStatus;
                                        ServiceProviders[x].UsersWithIncidents = data[i].UsersWithIncidents;

                                    });
                                }
                                data.RemoveAt(i);
                                break;
                            }
                        }
                        if (remove)
                        {
                            InvokeIfNecessary(() => ServiceProviders.RemoveAt(x));
                        }
                    }

                    if (data.Count > 0)
                    {
                        InvokeIfNecessary(() => ServiceProviders.AddRange(data));

                    }
                }

            }
            catch
            {

            }
            finally
            {
                updatingISP = false;
            }

        }

        private void PopulateSimpleIncident(Task<List<SimpleIncident>> task)
        {
            if (task.Exception is not null)
            {
                LogException(task.Exception);
                return;
            }
            if (task.Result is List<SimpleIncident> data)
            {
                PopulateSimpleIncident(data);
            }

        }

        private void PopulateSimpleIncident(List<SimpleIncident> data)
        {

            if (data.Count == 0)
            {
                LastIncident = null;
                return;
            }

            LastIncident = data.OrderByDescending(o => o.Created).First();

        }


        bool updatingMalicousUsersChart;
        private void PopulateMalicousUsersChart(Task<List<LocationCount>> task)
        {
            if (task.Exception is not null)
            {
                LogException(task.Exception);
                return;
            }
            if (task.Result is List<LocationCount> data)
            {
                PopulateMalicousUsersChart(data);
            }

        }
        private void PopulateMalicousUsersChart(List<LocationCount> data)
        {
            if (updatingMalicousUsersChart)
                return;
            SetBussyMessage($"Draw {data.Count:N0} visitors in abuse map");
            updatingMalicousUsersChart = true;
            try
            {
                if (AttackersMap.Count > 0)
                {
                    //update existing
                    for (int x = AttackersMap.Count - 1; x >= 0; x--)
                    {
                        bool remove = true;

                        for (int i = data.Count - 1; i >= 0; i--)
                        {
                            if (AttackersMap[x].CountryCode == data[i].CountryCode)
                            {
                                remove = false;
                                if (AttackersMap[x].Count != data[i].Count)
                                {
                                    InvokeIfNecessary(() => AttackersMap[x].Count = data[i].Count);
                                }
                                data.RemoveAt(i);
                                break;
                            }
                        }

                        if (remove)
                        {
                            InvokeIfNecessary(() => AttackersMap.RemoveAt(x));
                        }

                    }
                }

                if (data.Count > 0)
                {
                    InvokeIfNecessary(() => AttackersMap.AddRange(data));

                }

            }
            finally
            {
                updatingMalicousUsersChart = false;
            }
        }




        bool updatingStatsCounters;
        private void PopulateStatsCounters(Task<StatusCounters> task)
        {
            if (task.Exception is not null)
            {
                LogException(task.Exception);
                return;
            }

            if (task.Result is StatusCounters counter)
            {
                PopulateStatsCounters(counter);
            }
        }


        public string DateRange
        {
            get
            {
                var date = DateTime.Now.Subtract(_desktop.InitializeTimeRange);
                var label = (date.Date.DayOfYear == DateTime.Now.DayOfYear)

                    ? string.Concat(date.ToString(CultureInfo.CurrentUICulture.DateTimeFormat.ShortTimePattern), " till ", DateTime.Now.ToString(CultureInfo.CurrentUICulture.DateTimeFormat.ShortTimePattern))
                    : string.Concat(date.ToString(CultureInfo.CurrentUICulture.DateTimeFormat.LongDatePattern), " ", date.ToString(CultureInfo.CurrentUICulture.DateTimeFormat.ShortTimePattern), " till ", DateTime.Now.ToString(CultureInfo.CurrentUICulture.DateTimeFormat.ShortTimePattern));

                return string.Concat(_desktop.InitializeTimeTicks, " ", _desktop.TimeScale);
            }

        }
        public TimeScale Scale { get => _desktop.TimeScale; }

        public int TimeTicks
        {
            get => _desktop.InitializeTimeTicks;
            set
            {
                if (_desktop.InitializeTimeTicks != value)
                {
                    _desktop.InitializeTimeTicks = value;
                    base.RaisePropertyChanged(nameof(DateRange));
                    PerformRefresh();
                }
            }

        }
        private void PopulateStatsCounters(StatusCounters counter)
        {
            if (updatingStatsCounters)
                return;
            updatingStatsCounters = true;
            try
            {

                if (NewUsers != counter.NewUsers)
                {
                    try
                    {
                        _service.GetStatusVisitors(_desktop.InitializeTimeRange, PageTokenSource.Token)
                                .ContinueWith((task) => PopulateVisitorsChart(task), PageTokenSource.Token)
                                .ConfigureAwait(true);
                    }
                    catch (Exception e)
                    {
                        LogException(e);
                    }
                }

                MaliciousUserAgentsCount = counter.MaliciousUserAgents;
                VisitsBySearchEnginesCount = counter.VisitsBySearchEngines;
                NewUsers = counter.NewUsers;
                BlockedUsers = counter.BlockedUsers;
                PortBasedAttacks = counter.PortBasedAttacks;

            }
            finally
            {
                updatingStatsCounters = false;
            }
        }

        bool updatingVisitorsChart;
        private void PopulateVisitorsChart(Task<List<LocationCount>> task)
        {
            if (task.Exception is not null)
            {
                LogException(task.Exception);
                return;
            }
            if (task.Status == TaskStatus.Canceled)
                return;



            if (task.Result is List<LocationCount> data)
            {
                PopulateVisitorsChart(data);

            }


        }

        private void PopulateVisitorsChart(List<LocationCount> data)
        {
            if (updatingVisitorsChart)
                return;
            SetBussyMessage($"Draw {data.Count:N0} visitors in map");
            updatingVisitorsChart = true;
            try
            {
                if (data.Count == 0)
                    return;



                if (VisitorsMap.Count > 0)
                {
                    for (int x = VisitorsMap.Count - 1; x >= 0; x--)
                    {
                        bool remove = true;

                        for (int i = data.Count - 1; i >= 0; i--)
                        {
                            if (VisitorsMap[x].CountryCode == data[i].CountryCode)
                            {
                                remove = false;

                                if (VisitorsMap[x].Count != data[i].Count)
                                {
                                    InvokeIfNecessary(() => VisitorsMap[x].Count = data[i].Count);
                                }
                                data.RemoveAt(i);
                                break;
                            }
                        }

                        if (remove)
                        {
                            InvokeIfNecessary(() => VisitorsMap.RemoveAt(x));
                        }

                    }
                }

                if (data.Count > 0)
                {
                    InvokeIfNecessary(() => VisitorsMap.AddRange(data));
                }
            }
            finally
            {
                updatingVisitorsChart = false;
            }
        }

        private void Populate(Task<DesktopData> task)
        {
            var errors = Errors.Count;
            if (task.Exception is not null)
            {
                LogException(task.Exception);
                return;
            }
            if (task.Status == TaskStatus.Canceled)
            {
                IsLoading = false;
                return;
            }

            if (task.Result is DesktopData data)
            {
                Populate(data);
            }
            else
            {
                var dlgService = _containerProvider.Resolve<IDialogService>();
                if (Errors.Count - errors == 0)
                {
                    dlgService.ShowMessageDialog($"Somehow {RunTime.SelectedFireWall.DisplayName} returned no data, please make sure the {RunTime.SelectedFireWall.Domain} is active.", "No-Data");
                }
                else
                {
                    if (dlgService.ShowMessageDialog($"Somehow {RunTime.SelectedFireWall.DisplayName} has thrown {Errors.Count - errors}, Would you like to view them?.", "Errors detected", icon: System.Windows.MessageBoxImage.Question, buttons: System.Windows.MessageBoxButton.YesNo) == ButtonResult.Yes)
                    {

                        var p = new DialogParameters{

                            { ErrorsDialigViewModel.OBJECT, Errors }
                        };

                        dlgService.ShowDialog(DialogNames.ERRORS, parameters: p, callback);
                    }
                }
            }
            IsLoading = false;
            void callback(IDialogResult obj)
            {
                if (obj.Result == ButtonResult.OK)
                {
                    Errors.Clear();
                }
            }
        }

        public RequestStats RequestStats { get; private set; }

        public SimpleRequest LastRequest
        {
            get => _lastRequest;
            set
            {
                if (SetProperty(ref _lastRequest, value))
                {
                    ExpandVisitors.RaiseCanExecuteChanged();
                }
            }
        }

        public IncedentStats IncedentStats { get; private set; }

        public SimpleIncident LastIncident
        {
            get { return _lastIncident; }
            set
            {

                if (SetProperty(ref _lastIncident, value))
                {
                    ExpandIncidents.RaiseCanExecuteChanged();
                }
            }
        }


        private void Populate(DesktopData data)
        {
            if (data is null)
                return;


            if (data.RequestStats is not null)
            {
                SetBussyMessage("update request stats");
                RequestStats = data.RequestStats;

                LastRequest = data.RequestStats.LastRequest;

                ObservingBots = data.RequestStats.Bots.ToKString();
                ObservingHumans = data.RequestStats.Humans.ToKString();
                ObservingSearchEngines = data.RequestStats.SearchEngines.ToKString();
            }


            if (data.IncedentStats is not null)
            {
                SetBussyMessage("update incident stats");
                this.IncedentStats = data.IncedentStats;
                LastIncident = data.IncedentStats.LastIncident;
                BlockedMalicious = data.IncedentStats.BlockedRequests.ToKString();
                ObservingMalicious = data.IncedentStats.UsersWithIncedents.ToKString();
            }

            try
            {

                this.MaliciousUserAgentsCount = data.Administration.MaliciousUserAgents;
                this.VisitsBySearchEnginesCount = data.Administration.VisitsBySearchEngines;
                this.NewUsers = data.Administration.NewUsers;
                this.BlockedUsers = data.Administration.BlockedUsers;
                this.PortBasedAttacks = data.Administration.PortBasedAttacks;
            }
            catch
            { }

            try
            {
                this.LicenseStatus = data.Status.LicenseStatus;
                this.UpTime = data.Status.UpTime;
                this.Visitors = data.Status.Visitors;
                this.Incidents = data.Status.Attacks;
                this.ServerTime = data.Status.ServerDate;
            }
            catch
            {
            }

            try
            {
                
                PopulateVisitorsChart(data.Visitors);
                var fist = data.Visitors.OrderByDescending(o => o.Count).FirstOrDefault();
                if (fist is not null)
                {
                    VisitorMapCenter = new System.Windows.Point(fist.MapLocation.Lat, fist.MapLocation.Lat);
                }
            }
            catch
            { }

            try
            {
                PopulateMalicousUsersChart(data.Incidents);
            }
            catch
            { }

            try
            {
                PopulateISPGrid(data.ISP.ISP);
            }
            catch
            { }

            //if (_timer is not null && !_timer.Enabled)
            //{
            //    _timer.Start();
            //}
            SetBussyMessage(string.Empty);
            IsLoading = false;

        }
        public async void OnNavigatedTo(NavigationContext navigationContext)
        {
            if (PageTokenSource is null)
            {
                PageTokenSource = new CancellationTokenSource();
            }

            SetBussyMessage("Loading..");
            try
            {
                var data = await _service.GetInitialized(_desktop.InitializeTimeRange, externalToken: PageTokenSource.Token);
                Populate(data);
            }
            finally
            {
                IsLoading = false;
            }
            //.ContinueWith(Populate, PageTokenSource.Token).ConfigureAwait(false);






        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {

            if (_timer is not null)
            {
                _timer.Elapsed -= RefreshHeader;
                _timer.Stop();
                _timer.Dispose();
                _timer = null;
            }

            if (PageTokenSource is not null)
            {
                PageTokenSource.Cancel();
                PageTokenSource.Dispose();
                PageTokenSource = null;
            }

        }

        private string serverCountry;

        private SimpleRequest _lastRequest;
        private SimpleIncident _lastIncident;

        public string ServerCountry { get => serverCountry; set => SetProperty(ref serverCountry, value); }


        private bool IsISPSelected(SimpleISP arg)
        {
            return selectedIspItem is not null;
        }

        private void PerformFireWallAction(SimpleISP obj)
        {
            var dlgService = _containerProvider.Resolve<IDialogService>();
            var p = new DialogParameters
            {
                { QuickActionViewModel.OBJECT, selectedIspItem },

            };
            dlgService.ShowDialog(DialogNames.QUICKACTION, parameters: p, callback);
            void callback(IDialogResult obj)
            {

            }
        }


        private SimpleISP selectedIspItem;
        private bool _isLoading;
        private bool _autoRefresh;
        private TimeSpan _autoRefreshInterval = TimeSpan.FromSeconds(10);

        public DelegateCommand<SimpleISP> FireWallAction { get; private set; }
        public DelegateCommand<SimpleISP> ISPDetail { get; private set; }
        public SimpleISP SelectedIspItem
        {
            get => selectedIspItem;
            set
            {
                if (SetProperty(ref selectedIspItem, value))
                {
                    FireWallAction?.RaiseCanExecuteChanged();
                    ISPDetail.RaiseCanExecuteChanged();
                    GenerateAbuseReport.RaiseCanExecuteChanged();
                }
            }
        }



        void SetBussyMessage( string message)
        {
            BussyMessage = message;
            IsLoading = !string.IsNullOrEmpty(message);
        }

        public string BussyMessage { get => _bussyMessage; set =>SetProperty(ref _bussyMessage , value); }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                SetProperty(ref _isLoading, value);
                RefreshNow.RaiseCanExecuteChanged();
                IsNotBusy = !IsLoading;
                if (value)
                {
                    _event.Subscribe(SetLoggingActivityAction);
                }
                else
                {
                    _event.Unsubscribe(SetLoggingActivityAction);
                }
            }
        }


        private void SetLoggingActivityAction(RequestsLog log)
        {
            if (_requestsLog is null && log is not null)
            {
                _requestsLog = log;
                _requestsLog.OnChanged += OnLogWebRequest;
            }

        }
        private void OnLogWebRequest(string statusMessage)
        {
            SetBussyMessage(statusMessage);
        }
        private RequestsLog _requestsLog;
        private string blockedMalicious;
        private string observingMalicious;
        public string BlockedMalicious { get => blockedMalicious; set => SetProperty(ref blockedMalicious, value); }
        public string ObservingMalicious { get => observingMalicious; set => SetProperty(ref observingMalicious, value); }

        private string _observingSearchEngines;
        private string _observingHumans;
        private string _observingBots;

        public string ObservingSearchEngines { get => _observingSearchEngines; set => SetProperty(ref _observingSearchEngines, value); }
        public string ObservingHumans { get => _observingHumans; set => SetProperty(ref _observingHumans, value); }
        public string ObservingBots { get => _observingBots; set => SetProperty(ref _observingBots, value); }

        private System.Windows.Point visitorMapCenter;
        private bool _changeGrouping;

        public System.Windows.Point VisitorMapCenter { get => visitorMapCenter; set => SetProperty(ref visitorMapCenter, value); }

        private bool isNotBusy;
        private string _bussyMessage;

        public bool IsNotBusy { get => isNotBusy; set => SetProperty(ref isNotBusy, value); }

    }
}
