using Desktop.Core;
using Desktop.Model;
using Desktop.Model.Events;
using Desktop.Services.Interfaces;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.Grid.Converter;
using Syncfusion.XlsIO;
using System;
using System.Collections;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
namespace Desktop.Dialogs
{
    public abstract class DialogBase : BindableBase, IDialogAware
    {

        private readonly IDialogService _dialogService;
        private readonly FireWallRequestsLogEvent _event;
        private readonly IDataService _service;
        private bool _isBusy;
        private string _loadingProgress = "loading...";
        private RequestsLog _requestsLog;
        private string _title;

        public DialogBase(IDialogService dialogService, IDataService kpiService, IEventAggregator eventAggregator)
        {
            DlgCommand = new DelegateCommand(CloseDialogCancel);
            
            ExportToExcel = new DelegateCommand<SfDataGrid>(ExecuteExportToExcel, CanExportToExcel);
            PageTokenSource = new CancellationTokenSource();
            if (!string.IsNullOrEmpty(RunTime.SelectedFireWall?.DisplayName))
            {
                Title = $"{RunTime.SelectedFireWall?.DisplayName ?? string.Empty} Firewall Management Dashboard";
            }
            else
            {
                Title = "Firewall Management Dashboard";
            }

            _event = eventAggregator.GetEvent<FireWallRequestsLogEvent>();
            _service = kpiService;
            _dialogService = dialogService;
        }
        public event Action<IDialogResult> RequestClose;


        protected void Fire_RequestClose(object value)
        {
            var p = new DialogParameters();
            p.Add(DialogParameterNames.DIALOG_RESULT, value);
            RequestClose?.Invoke(new DialogResult(ButtonResult.OK, p));
        }
        protected void Fire_RequestClose(ButtonResult result, DialogParameters args)
        {                        
            RequestClose?.Invoke(new DialogResult(result, args));
        }


        /// <summary>
        /// Data-bound to the default cancel/close button will close the Dialog 
        /// </summary>
        public DelegateCommand DlgCommand { get; private set; }
        public DelegateCommand<SfDataGrid> ExportToExcel { get; private set; }

        /// <summary>
        /// Set the model to busy indicating to the view that loading is in progress
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            protected set
            {

                InvokeIfNecessary(() => SetProperty(ref _isBusy, value));
                if (value)
                {
                    _event.Subscribe(SetAction);
                }
                else
                {
                    _event.Unsubscribe(SetAction);
                }
            }
        }
        /// <summary>
        /// Busy progress text
        /// </summary>
        public string LoadingProgress
        {
            get => _loadingProgress;
            set
            {
                if (InvokeRequired())
                {
                    Invoke(() => SetProperty(ref _loadingProgress, value));
                }
                else
                {
                    SetProperty(ref _loadingProgress, value);
                }
            }
        }

        /// <summary>
        /// Dialog title
        /// </summary>
        public string Title { get => _title; protected set => SetProperty(ref _title, value); }

        /// <summary>
        /// access the firewall data service
        /// </summary>
        protected IDataService DataService { get => _service; }


        /// <summary>
        /// access the dialog service allowing you to open registered dialogs
        /// </summary>
        protected IDialogService DialogService { get => _dialogService; }

        /// <summary>
        /// cancellation token used to cancel any active requests that are not needed when the user closes a dialog
        /// </summary>
        protected CancellationTokenSource PageTokenSource { get; private set; }

        /// <summary>
        /// Indicates that the dialog can close
        /// </summary>
        /// <returns></returns>
        public virtual bool CanCloseDialog()
        {
            return true;
        }

        /// <summary>
        /// free Disposable members and un-registers subscribed events
        /// </summary>
        public virtual void OnDialogClosed()
        {
            PageTokenSource?.Cancel();
            PageTokenSource?.Dispose();
            PageTokenSource = null;

            _event.Unsubscribe(SetAction);
            if (_requestsLog is not null)
            {
                _requestsLog.OnChanged -= OnLogWebRequest;
                _requestsLog = null;
            }
        }

        /// <summary>
        /// overwrite the actions to populate the dialog
        /// </summary>
        /// <param name="parameters"></param>
        public abstract void OnDialogOpened(IDialogParameters parameters);


        /// <summary>
        /// Will synchronism background threads and UI threads when updating is needed
        /// </summary>
        /// <param name="action">The action to execute on the UI thread</param>
        protected void InvokeIfNecessary(Action action)
        {
            if (InvokeRequired())
            {
                System.Windows.Application.Current.Dispatcher.Invoke(action);
            }
            else
            {
                action();
            }
        }

        protected void Invoke(Action action)
        { 
            System.Windows.Application.Current.Dispatcher.Invoke(action);
        }

        /// <summary>
        /// Used to test if the action is on a background thread
        /// </summary>
        /// <returns></returns>
        protected bool InvokeRequired() => Thread.CurrentThread != System.Windows.Application.Current.Dispatcher.Thread;


        /// <summary>
        /// Show the WHOIS data for a Internet Service Provider that is managing the IP address
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <returns></returns>
        protected async Task ISPDetails(string ipAddress)
        {
            IsBusy = true;

            try
            {
                if (System.Net.IPAddress.TryParse(ipAddress, out _))
                {
                    var whois = await _service.GetWhois(ipAddress);

                    var p = new DialogParameters
                    {
                        { WHOISDialogViewModel.IWHOIS, whois },
                        { WHOISDialogViewModel.TITLE, $"{whois.Organization} Details" }
                    };
                    _dialogService.ShowDialog(DialogNames.ISPDETAIL_DIALOGUE, parameters: p, callback);
                    void callback(IDialogResult obj)
                    {

                    }
                }
                else
                {
                    throw new ArgumentException(nameof(ipAddress));
                }
            }
            finally
            {

                IsBusy = false;
            }

        }

        private void OnLogWebRequest(string statusMessage)
        {
            LoadingProgress = statusMessage;
        }

        private void CloseDialogOK()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
        }
        private void CloseDialogCancel()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
        }
        private void SetAction(RequestsLog log)
        {
            if (_requestsLog is null && log is not null)
            {
                _requestsLog = log;
                _requestsLog.OnChanged += OnLogWebRequest;
            }

        }

        private bool CanExportToExcel(SfDataGrid grid)
        {
            return grid is not null
                && grid.ItemsSource is ICollection items
                && items.Count > 0;
        }
        protected void ExecuteExportToExcel(SfDataGrid grid)
        {

            var sfd = new SaveFileDialog
            {
                FilterIndex = 2,
                Filter = "Excel 97 to 2003 Files(*.xls)|*.xls|Excel 2007 to 2010 Files(*.xlsx)|*.xlsx|Excel 2013 File(*.xlsx)|*.xlsx"
            };
            var options = new ExcelExportingOptions() { ExcelVersion = ExcelVersion.Excel2013 };


            using var excelEngine = grid.ExportToExcel(grid.View, excelExportingOptions: options);
            var workBook = excelEngine.Excel.Workbooks[0];

            if (sfd.ShowDialog() == true)
            {
                using (Stream stream = sfd.OpenFile())
                {

                    if (sfd.FilterIndex == 1)
                    {
                        workBook.Version = ExcelVersion.Excel97to2003;
                    }
                    else if (sfd.FilterIndex == 2)
                    {
                        workBook.Version = ExcelVersion.Excel2010;
                    }
                    else
                    {
                        workBook.Version = ExcelVersion.Excel2013;
                    }


                    workBook.SaveAs(stream);
                }

            }


        }
    }

}
