using Desktop.Core.Dialogs;
using Desktop.Model;
using Desktop.Model.Desktop;
using Desktop.Services.Interfaces;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Walter.BOM;
using Walter.BOM.Geo;

namespace Desktop.Dialogs
{
    public class QuickActionViewModel : BindableBase, IDialogAware
    {

        internal const string OBJECT = "OBJ";
        private readonly IDialogService _dialogService;
        private readonly IDataService _service;
        private Visibility _abuseReportVisible;
        private string _capebilities;
        SimpleCountry _country;
        private GeoLocation _countryCode;
        private Visibility _geoEnabled = Visibility.Visible;
        private string _geograhyBlockedTill = " - ";
        private BlockFeature _geographyBlockFeature;
        SimpleIP _ip;
        private string _iPADDress;
        private string _iPBlockedTill = " - ";
        private BlockFeature _iPBlockFeature;
        private Visibility _ipEnabled = Visibility.Visible;
        private string _iSPBlockedTill = " - ";
        private BlockFeature _iSPBlockFeature;
        private Visibility _ispEnabled = Visibility.Visible;
        private ManagementTypes _managementTypes;
        private object _selectedItem;
        private object _selectedPropertyGridItem;
        private string _selectedType;
        SimpleUser _user;
        private string _userBlockedTill = " - ";
        private BlockFeature _userBlockFeature;
        private Visibility _userEnabled = Visibility.Visible;
        private long _userId;
        private string _viewingTypeTitle;
        SimpleWhois _who;
        private string iSPAbuseMail;
        private string iSPName;
        CancellationTokenSource PageTokenSource;
        private DateTime _blockDate = DateTime.Now.AddDays(31);

        public QuickActionViewModel(IDialogService dialogService, IDataService service)
        {
            DlgCommand = new DelegateCommand<object>(CloseDialogOK, CanSaveChanges);
            CancelDlgCommand = new DelegateCommand(CloseDialogCancel);
            GetUser = new DelegateCommand(CallGetUser);
            GetIP = new DelegateCommand(CallGetIP);
            GetISP = new DelegateCommand(CallGetISP);
            BackToSelectedItem = new DelegateCommand(CallLoadSelectedItemAgain);
            GetCountry = new DelegateCommand(CallGetCountry);
            AddPayload = new DelegateCommand(CallAddPayload, CanAddPayload);
            OpenFileCommand = new DelegateCommand(CallSelectPayLoadFile);
            _dialogService = dialogService;
            _service = service;
            PossibleResponceExpected = new List<ResponceExpected>() {
                ResponceExpected.FileStream,
                ResponceExpected.GZip,
                ResponceExpected.SevenZip,
                ResponceExpected.Zip,
                ResponceExpected.Json,
                ResponceExpected.XML,
                ResponceExpected.Html,
                ResponceExpected.Png,
                ResponceExpected.Jpeg,
                ResponceExpected.MP4,
                ResponceExpected.MOV
            };

        }



        private void CallAddPayload()
        {
            ResponceExpected responce = ResponceExpected.None;
            foreach (var item in selectedResponce)
            {
                if (item is ResponceExpected r)
                {
                    responce |= r;
                }
            }
            PayLoads.Add(new SimplePayLoad() { FileName = FileName, AssignTo = PayloadTarget.User | PayloadTarget.IPAddress, Responce = responce });
        }

        public event Action<IDialogResult> RequestClose;

        public Visibility AbuseReportVisible { get => _abuseReportVisible; set => SetProperty(ref _abuseReportVisible, value); }

        public DelegateCommand BackToSelectedItem { get; private set; }
        public DelegateCommand AddPayload { get; private set; }
        public DelegateCommand OpenFileCommand { get; set; }

        public DelegateCommand CancelDlgCommand { get; private set; }
        public List<ResponceExpected> PossibleResponceExpected { get; }

        public string Capebilities { get => _capebilities; set => SetProperty(ref _capebilities, value); }

        public long CIDR { get; set; }

        public DelegateCommand<object> DlgCommand { get; private set; }

        public Visibility GeoEnabled { get => _geoEnabled; set => SetProperty(ref _geoEnabled, value); }

        public string GeograhyBlockedTill { get => _geograhyBlockedTill; set => SetProperty(ref _geograhyBlockedTill, value); }

        public BlockFeature GeographyBlockFeature
        {
            get => _geographyBlockFeature;
            set
            {
                if (SetProperty(ref _geographyBlockFeature, value))
                {
                    DlgCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public DelegateCommand GetCountry { get; private set; }

        public DelegateCommand GetIP { get; private set; }

        public DelegateCommand GetISP { get; private set; }

        public DelegateCommand GetUser { get; private set; }

        public string IPAddress { get => _iPADDress; set => SetProperty(ref _iPADDress, value); }

        public string IPBlockedTill { get => _iPBlockedTill; set => SetProperty(ref _iPBlockedTill, value); }

        public BlockFeature IPBlockFeature { get => _iPBlockFeature; set => SetProperty(ref _iPBlockFeature, value); }

        public Visibility IPEnabled { get => _ipEnabled; set => SetProperty(ref _ipEnabled, value); }

        public string ISPAbuseMail
        {
            get => iSPAbuseMail;
            set
            {
                SetProperty(ref iSPAbuseMail, value);
                AbuseReportVisible = string.IsNullOrEmpty(value) ? Visibility.Collapsed : Visibility.Visible;

            }
        }

        public string ISPBlockedTill { get => _iSPBlockedTill; set => SetProperty(ref _iSPBlockedTill, value); }

        public BlockFeature ISPBlockFeature { get => _iSPBlockFeature; set => SetProperty(ref _iSPBlockFeature, value); }

        public Visibility ISPEnabled { get => _ispEnabled; set => SetProperty(ref _ispEnabled, value); }

        public string ISPName { get => iSPName; set => SetProperty(ref iSPName, value); }

        public string Location
        {
            get => _countryCode.GetCountryName();
        }

        public ManagementTypes ManagementTypes
        {
            get => _managementTypes;
            set
            {
                if (SetProperty(ref _managementTypes, value))
                {
                    switch (_managementTypes)
                    {
                        case ManagementTypes.None:
                            break;
                        case ManagementTypes.User:
                            GeoEnabled = Visibility.Visible;
                            UserEnabled = Visibility.Visible;
                            IPEnabled = Visibility.Visible;
                            break;
                        case ManagementTypes.IPAddress:
                            GeoEnabled = Visibility.Visible;
                            UserEnabled = Visibility.Collapsed;
                            IPEnabled = Visibility.Visible;
                            ISPEnabled = Visibility.Visible;
                            break;
                        case ManagementTypes.ISP:
                            GeoEnabled = Visibility.Visible;
                            ISPEnabled = Visibility.Visible;
                            UserEnabled = Visibility.Collapsed;
                            IPEnabled = Visibility.Collapsed;

                            break;
                        case ManagementTypes.UserAgent:
                            GeoEnabled = Visibility.Collapsed;
                            UserEnabled = Visibility.Collapsed;
                            UserEnabled = Visibility.Collapsed;

                            ISPEnabled = Visibility.Visible;
                            break;
                        case ManagementTypes.Endpoint:
                            GeoEnabled = Visibility.Collapsed;
                            UserEnabled = Visibility.Collapsed;
                            ISPEnabled = Visibility.Visible;

                            break;
                        case ManagementTypes.Request:
                            GeoEnabled = Visibility.Visible;
                            UserEnabled = Visibility.Visible;
                            IPEnabled = Visibility.Visible;
                            ISPEnabled = Visibility.Visible;

                            break;
                        case ManagementTypes.Incident:
                            GeoEnabled = Visibility.Visible;
                            UserEnabled = Visibility.Visible;
                            IPEnabled = Visibility.Visible;
                            ISPEnabled = Visibility.Visible;
                            break;
                        case ManagementTypes.PhishyRequest:
                            GeoEnabled = Visibility.Visible;
                            UserEnabled = Visibility.Visible;
                            IPEnabled = Visibility.Visible;
                            ISPEnabled = Visibility.Visible;
                            break;
                        case ManagementTypes.Port:
                            GeoEnabled = Visibility.Visible;
                            UserEnabled = Visibility.Visible;
                            IPEnabled = Visibility.Visible;
                            ISPEnabled = Visibility.Visible;
                            break;
                        case ManagementTypes.Process:
                            GeoEnabled = Visibility.Collapsed;
                            UserEnabled = Visibility.Collapsed;
                            IPEnabled = Visibility.Visible;
                            ISPEnabled = Visibility.Visible;
                            break;
                    }
                }
            }
        }

        public object SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value))
                {
                    SelectedPropertyGridItem = value;
                }
            }
        }

        public object SelectedPropertyGridItem { get => _selectedPropertyGridItem; set => SetProperty(ref _selectedPropertyGridItem, value); }

        public string SelectedType { get => _selectedType; set => SetProperty(ref _selectedType, value); }

        public string Title { get; private set; }
        public DateTime BlockDate { get => _blockDate; set => SetProperty(ref _blockDate, value); }
        public string UserBlockedTill { get => _userBlockedTill; set => SetProperty(ref _userBlockedTill, value); }

        public BlockFeature UserBlockFeature
        {
            get => _userBlockFeature;
            set
            {
                if (SetProperty(ref _userBlockFeature, value))
                {
                    DlgCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public Visibility UserEnabled { get => _userEnabled; set => SetProperty(ref _userEnabled, value); }

        public long UserId { get => _userId; set => SetProperty(ref _userId, value); }

        public string ViewingTypeTitle { get => _viewingTypeTitle; set => SetProperty(ref _viewingTypeTitle, value); }

        GeoLocation CountryCode
        {
            get => _countryCode;
            set
            {
                if (SetProperty(ref _countryCode, value))
                {
                    base.RaisePropertyChanged(nameof(Location));

                }
            }
        }

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
            PageTokenSource.Dispose();


        }


        public void OnDialogOpened(IDialogParameters parameters)
        {
            PageTokenSource = new CancellationTokenSource();

            SelectedItem = parameters.GetValue<object>(OBJECT);
            if (SelectedItem is SimpleRequest sr)
            {

                SelectedType = "Request";
                Capebilities = $"You can manage connectivity for future requests coming from the user and configure the firewall to block or allow communications based on properties of the user in the request.";
                UserId = sr.FWUID;
                CountryCode = sr.Location;
                CIDR = sr.CIDR;
                this.IPAddress = sr.IPAddress;
                Title = "Request configuration";
                ManagementTypes = Walter.BOM.ManagementTypes.Endpoint | Walter.BOM.ManagementTypes.ISP | Walter.BOM.ManagementTypes.IPAddress | Walter.BOM.ManagementTypes.User;
            }
            else if (SelectedItem is SimpleIncident si)
            {

                CIDR = si.CIDR;
                this.IPAddress = si.IPAddress;
                SelectedType = "Incident";
                Capebilities = $"You can manage connectivity for future requests coming from the user or users IP address and configure the firewall to block or allow communications based on properties of the user causing the incident.";
                UserId = si.FWUID;
                CountryCode = si.Location;
                Title = "Incident configuration";
                ManagementTypes = Walter.BOM.ManagementTypes.Incident | Walter.BOM.ManagementTypes.Endpoint | Walter.BOM.ManagementTypes.ISP | Walter.BOM.ManagementTypes.IPAddress | Walter.BOM.ManagementTypes.User;
            }
            else if (SelectedItem is SimpleISP sp)
            {

                CIDR = sp.CIDR_From;
                SelectedType = "Internet Service Provider";
                if (!string.IsNullOrEmpty(sp.Organization))
                {
                    Capebilities = $"You can manage connectivity for requests coming from {sp.Organization} and block or allow communications with any of the IP addresses in the {sp.IP_From} - {sp.IP_Till} range. ";
                }
                else
                {
                    Capebilities = $"You can manage connectivity for future requests coming from the service provider and block or allow communications with any of the IP addresses in the {sp.IP_From} - {sp.IP_Till} range. ";
                }
                ISPAbuseMail = sp.Abuse;
                CountryCode = sp.Location;
                Title = "Internet Service Provider configuration";
                ManagementTypes = ManagementTypes.ISP;

            }



            var options = new BlacklistStatusCommand()
            {
                CIDR = this.CIDR,
                Location = this.CountryCode,
                UserId = this.UserId,
                ISPCidr = CIDR
            };
            _service.GetBlockStatus(options, PageTokenSource.Token)
                     .ContinueWith(SetOptions, PageTokenSource.Token);

        }

        public ObservableCollection<SimplePayLoad> PayLoads { get; set; } = new();
        private async void CallGetCountry()
        {
            if (_country is null)
            {
                _country = await _service.GetSimpleCountry(CountryCode, PageTokenSource.Token);
            }
            SelectedPropertyGridItem = _country;

            if (_user is not null)
            {
                ViewingTypeTitle = $"-> Country ({CountryCode.GetCountryName()})";
            }
        }

        private async void CallGetIP()
        {
            if (_ip is null)
            {
                _ip = await _service.GetSimpleIP(cidr: CIDR, PageTokenSource.Token);
            }
            SelectedPropertyGridItem = _ip;

            if (_ip is not null)
            {
                ViewingTypeTitle = $"-> IP Address {_ip.IPAddress}";
            }

        }

        private async void CallGetISP()
        {
            if (_who is null)
            {
                var item = await _service.GetWhois(cidr: CIDR, PageTokenSource.Token);

                _who = new SimpleWhois(item);

            }
            SelectedPropertyGridItem = _who;

            if (_who is not null)
            {
                ViewingTypeTitle = $"-> ISP {_who.Organization}";

                ISPAbuseMail = _who.ContactDetails.Abuse;
            }
        }

        private async void CallGetUser()
        {
            if (_user is null)
            {
                _user = await _service.GetSimpleFireWallUser(UserId, PageTokenSource.Token);
            }
            SelectedPropertyGridItem = _user;

            if (_user is not null)
            {
                ViewingTypeTitle = $"-> User {_user.UserType}";
            }

        }

        private void CallLoadSelectedItemAgain()
        {
            SelectedPropertyGridItem = SelectedItem;
            ViewingTypeTitle = string.Empty;
        }

        private bool CanSaveChanges(object arg)
        {
            return _userBlockFeature != BlockFeature.NotSet
                || _geographyBlockFeature != BlockFeature.NotSet;
        }
        private void CloseDialogCancel()
        {
            PageTokenSource.Cancel();
            RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
        }

        private void CloseDialogOK(object arg)
        {
            PageTokenSource.Cancel();

            if (RunTime.SelectedFireWall?.User is not null
               && (RunTime.SelectedFireWall.User.Role.HasFlag(AdminAccess.Guest) || RunTime.SelectedFireWall.User.Role.HasFlag(AdminAccess.ReadOnly)))
            {
                _dialogService.PemissionDenied();
                return;
            }

            if (Enum.TryParse<DialogButton>(arg.ToString(), out var buttonResult))
            {
                var dialogResult = buttonResult switch
                {
                    DialogButton.OK => ButtonResult.OK,
                    DialogButton.Cancel => ButtonResult.Cancel,
                    DialogButton.No => ButtonResult.No,
                    DialogButton.Yes => ButtonResult.Yes,
                    _ => ButtonResult.OK,
                };

                RequestClose?.Invoke(new DialogResult(dialogResult));
            }
            else
            {
                throw new NotSupportedException($"The value of {arg} is not supported in Enum DialogButton");
            }
        }

        private void SetOptions(Task<BlockStatusState> task)
        {
            if (task.Exception is not null)
            {
                _dialogService.ShowMessageDialog("Getting the current block status failed", "Query error");
                return;
            }

            if (task.Result is BlockStatusState state)
            {
                if (state.Location is not null)
                {

                    GeographyBlockFeature = state.Location.Source == BlaklistSource.None ? BlockFeature.NotSet : BlockFeature.Block;
                    if (GeographyBlockFeature == BlockFeature.Block)
                    {
                        GeograhyBlockedTill = new DateTime(state.Location.Expires, DateTimeKind.Utc).ToLocalTime().ToString();
                    }

                }

                if (state.UserId is not null)
                {

                    UserBlockFeature = state.UserId.Source == BlaklistSource.None ? BlockFeature.NotSet : BlockFeature.Block;
                    if (UserBlockFeature == BlockFeature.Block)
                    {
                        UserBlockedTill = new DateTime(state.UserId.Expires, DateTimeKind.Utc).ToLocalTime().ToString();
                    }

                }
                if (state.CIDR is not null)
                {

                    IPBlockFeature = state.CIDR.Source == BlaklistSource.None ? BlockFeature.NotSet : BlockFeature.Block;
                    if (UserBlockFeature == BlockFeature.Block)
                    {
                        IPBlockedTill = new DateTime(state.CIDR.Expires, DateTimeKind.Utc).ToLocalTime().ToString();
                    }

                }

                if (state.ISPCidr is not null)
                {

                    ISPBlockFeature = state.ISPCidr.Source == BlaklistSource.None ? BlockFeature.NotSet : BlockFeature.Block;
                    if (ISPBlockFeature == BlockFeature.Block)
                    {
                        ISPBlockedTill = new DateTime(state.ISPCidr.Expires, DateTimeKind.Utc).ToLocalTime().ToString();
                    }

                }


            }

        }

        private DelegateCommand generateAbuseReport;
        public DelegateCommand GenerateAbuseReport => generateAbuseReport ??= new DelegateCommand(PerformGenerateAbuseReport);

        private void PerformGenerateAbuseReport()
        {
            var cmd = _who is not null
                ? new AbuseReportCommand() { DataSource = AbuseReportCommand.Filter.ISPRangeId, Value = _who.RangeId, BreadCrumbs = true, PortBasedAttacks = true, WebRequests = true }
                : new AbuseReportCommand() { DataSource = AbuseReportCommand.Filter.IPAddress, Value = CIDR, BreadCrumbs = false, PortBasedAttacks = true, WebRequests = true }
                ;
            var p = new DialogParameters()
            {
                { AbuseReportBuilderViewModel.COMMAND,cmd}
            };

            _dialogService.ShowDialog(DialogNames.ABUSE_REPORT, parameters: p, callback);
            void callback(IDialogResult obj)
            {

            }

        }

        private DateTime? payLoadDate;

        public DateTime? PayLoadDate { get => payLoadDate; set => SetProperty(ref payLoadDate, value); }

        private ObservableCollection<object> selectedResponce;
        private string fileName;

        public ObservableCollection<object> SelectedResponce
        {
            get => selectedResponce;
            set
            {
                if (SetProperty(ref selectedResponce, value))
                {
                    AddPayload.RaiseCanExecuteChanged();
                }

            }
        }

        public string FileName
        {
            get => fileName;
            set
            {
                if (SetProperty(ref fileName, value))
                {
                    AddPayload.RaiseCanExecuteChanged();
                }


            }
        }

        public DateTime MinDateTime => DateTime.Now.AddHours(1);

        private void CallSelectPayLoadFile()
        {
            var dlg = new OpenFileDialog();
            dlg.Title = "Select the payload you would like to provide";
            dlg.Multiselect = false;

            dlg.ShowDialog();
            if (File.Exists(dlg.FileName))
            {
                FileName = dlg.FileName;
            }
        }

        private bool CanAddPayload()
        {
            return SelectedResponce is not null && SelectedResponce.Count > 0 && PayLoadDate > DateTime.Now && File.Exists(FileName);
        }


    }
}
