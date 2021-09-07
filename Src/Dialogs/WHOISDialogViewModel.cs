using Desktop.Model;
using Desktop.Model.Desktop;
using Desktop.Services.Interfaces;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Walter.Net.Networking;

namespace Desktop.Dialogs
{
    public class WHOISDialogViewModel : BindableBase, IDialogAware
    {
        internal const string IWHOIS = "WHO";
        internal const string ISP = "ISP";
        internal const string CIDR = "CIDR";
        internal const string TITLE = "TITLE";
        private string _title;
        CancellationTokenSource PageTokenSource = new CancellationTokenSource();
        IDialogService _dialogService;
        IDataService _service;
        private SimpleWhois _wHOIS;
        private bool _showHints;

        public WHOISDialogViewModel(IDialogService dialogService, IDataService service)
        {

            _service = service;
            _dialogService = dialogService;
            FireWallAction = new DelegateCommand(PerformFireWallAction);
            OpenUrl = new DelegateCommand(PerformOpenUrl);
            DlgCommand = new DelegateCommand(CloseDialogOK,CloseCanAlter);
            DlgCommandCancel = new DelegateCommand(CloseDialogCancel);
            
        }

        private void CloseDialogCancel()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
        }

        private bool CloseCanAlter()
        {
            return !(RunTime.SelectedFireWall.User.Role.HasFlag(AdminAccess.Guest) | RunTime.SelectedFireWall.User.Role.HasFlag(AdminAccess.ReadOnly));
        }

        

        public DelegateCommand DlgCommandCancel { get; }
        private async void CloseDialogOK()
        {
            var action = new Model.Infrastructure.ActionCommand();
            if (WHOIS.TrustLevel == ISPTrustLevels.Report)
            {
                action.SetISP(WHOIS.SourceIPAddress.IP2Cidr(), null);
            }
            else
            { 
                action.SetISP(WHOIS.SourceIPAddress.IP2Cidr(), DateTime.Now.AddYears(10));
            }
            await _service.SetBlockStatus(action, PageTokenSource.Token);
            
            RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
            
        }
        private void PerformOpenUrl()
        {
            ProcessStartInfo sI = new ProcessStartInfo($"https://apps.db.ripe.net/db-web-ui/query?searchtext={WHOIS.IpAddresses.First().From}") { UseShellExecute = true };
            Process.Start(sI);
        }

        private void PerformFireWallAction()
        {

            var p = new DialogParameters
            {
                { QuickActionViewModel.OBJECT, WHOIS },

            };
            _dialogService.ShowDialog(DialogNames.QUICKACTION, parameters: p, callback);
            void callback(IDialogResult obj)
            {

            }
        }
        public bool ShowHints { get => _showHints; set =>SetProperty(ref _showHints , value); }
        public DelegateCommand DlgCommand { get; private set; }
        public DelegateCommand FireWallAction { get; private set; }
        public DelegateCommand OpenUrl { get; private set; }
        public string Title { get => _title; set => SetProperty(ref _title, value); }

        public event Action<IDialogResult> RequestClose;

        public bool CanCloseDialog()
        {
            return true;
        }
        public SimpleWhois WHOIS
        {
            get => _wHOIS;
            set => SetProperty(ref _wHOIS, value);
        }

        public void OnDialogClosed()
        {
            PageTokenSource.Cancel();
            PageTokenSource.Dispose();
            PageTokenSource = null;

        }

        public void OnDialogOpened(IDialogParameters parameters)
        {

            Title = "Loading WHOIS data for Internet service provider";
            if (parameters.ContainsKey(ISP))
            {
                var isp = parameters.GetValue<SimpleISP>(ISP);
                this.Title = $"Internet Service Provider:{isp.Organization} (Trust Level {isp.TrustLevel}/ last seen {isp.LastSeen.ToShortDateTime()})";
                _service.GetWhois(isp.CIDR_From, PageTokenSource.Token)
                    .ContinueWith(LoadWhois, PageTokenSource.Token);
            }
            else if (parameters.ContainsKey(CIDR))
            {
                _service.GetWhois(parameters.GetValue<long>(CIDR), PageTokenSource.Token)
                    .ContinueWith(LoadWhois, PageTokenSource.Token);

            }
            else if (parameters.ContainsKey(IWHOIS))
            {
                WHOIS = new SimpleWhois(parameters.GetValue<IWhois>(IWHOIS));
            }

        }

        private void LoadWhois(Task<IWhois> task)
        {
            if (task.Result is IWhois whois)
            {
                this.Title = $"Internet Service Provider:{whois.Organization} (Trust Level {whois.TrustLevel}/ last seen {whois.Counters.LastSeen.ToShortDateTime()})";

                WHOIS = new SimpleWhois(whois);
            }
        }
    }
}
