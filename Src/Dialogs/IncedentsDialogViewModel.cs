using Desktop.Core.Dialogs;
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
using System.Threading.Tasks;
using Walter.BOM;

namespace Desktop.Dialogs
{
    public class IncedentsDialogViewModel : DialogBase
    {
        internal const string TIME_SPAN = "TS";
        public const string TITLE = "T";

        private SimpleIncident _selectedItem;


        public IncedentsDialogViewModel(IDialogService dialogService, IDataService service, IEventAggregator eventAggregator)
            : base(dialogService, service, eventAggregator)
        {
            FireWallAction = new DelegateCommand<SimpleIncident>(PerformFireWallAction, IsItemSelected);
            Tracert = new DelegateCommand<SimpleIncident>(PerformTracert, IsItemSelected);
        }




        private ObservableCollection<SimpleIncident> incidents = new();
        RequestsLog _requestsLog;
        private void SetAction(RequestsLog obj)
        {

            _requestsLog = obj;
            _requestsLog.OnChanged += OnLogWebRequest;

        }

        private void OnLogWebRequest(string value)
        {
            LoadingProgress = value;
            Title = string.Concat(_orgTitle, " (", value, ")");
        }

        private void PerformTracert(SimpleIncident obj)
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

        private DelegateCommand generateAbuseReport;
        public DelegateCommand GenerateAbuseReport => generateAbuseReport ??= new DelegateCommand(PerformGenerateAbuseReport,CanGenerateAbuseReport);

        private bool CanGenerateAbuseReport()
        {
            return SelectedItem is not null;
        }

        private void PerformGenerateAbuseReport()
        {
            var cmd = new AbuseReportCommand() { DataSource = AbuseReportCommand.Filter.IPAddress, Value = SelectedItem.CIDR, BreadCrumbs = false, PortBasedAttacks = true, WebRequests = true };
                
            var p = new DialogParameters()
            {
                { AbuseReportBuilderViewModel.COMMAND,cmd}
            };

            DialogService.ShowDialog(DialogNames.ABUSE_REPORT, parameters: p, callback);
            void callback(IDialogResult obj)
            {

            }

        }


        public SimpleIncident SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value))
                {
                    FireWallAction.RaiseCanExecuteChanged();
                    Tracert.RaiseCanExecuteChanged();
                    GenerateAbuseReport.RaiseCanExecuteChanged();
                }
            }

        }
        public DelegateCommand<SimpleIncident> Tracert { get; private set; }
        public DelegateCommand<SimpleIncident> FireWallAction { get; private set; }


        public ObservableCollection<SimpleIncident> Incidents { get => incidents; set => SetProperty(ref incidents, value); }

        

        string _orgTitle;
        public override void OnDialogOpened(IDialogParameters parameters)
        {

            LoadingProgress = $"Binding parameters...";

            _orgTitle = parameters.GetValue<string>(TITLE);
            Title = string.Concat(_orgTitle, " (get data ....)");
            var time = parameters.GetValue<TimeSpan>(TIME_SPAN);


            IsBusy = true;
            DataService.GetSimpleIncident(time, PageTokenSource.Token).ContinueWith(PopulateIncidents, PageTokenSource.Token);

            if (time.TotalDays >= 1)
            {
                LoadingProgress = $"Incidents range {time.TotalDays:N1} days... ";
            }
            else if (time.TotalDays == 1)
            {
                LoadingProgress = $"Incidents range {time.TotalDays:N0} day... ";
            }
            else if (time.TotalHours >= 1)
            {
                LoadingProgress = $"Incidents range {time.TotalHours:N1} hours... ";
            }
            else if (time.TotalHours == 1)
            {
                LoadingProgress = $"Incidents range {time.TotalHours:N0} hour... ";
            }
            else
            {
                LoadingProgress = $"Incidents range {time.Minutes:N0} minutes... ";
            }



        }



        private void PopulateIncidents(Task<List<SimpleIncident>> task)
        {

            if (base.InvokeRequired())
            {
                InvokeIfNecessary(() => PopulateIncidents(task));
                return;
            }

            if (_requestsLog is not null)
            {
                _requestsLog.OnChanged -= OnLogWebRequest;
            }
            LoadingProgress = "Populating data ...";
            Title = _orgTitle;
            try
            {
                if (task.Exception is not null)
                {
                    DialogService.ShowMessageDialog($"Data request failed due to {task.Exception.Message}<br/>Please upgrade the software", $"{task.Exception.GetType().Name}", System.Windows.MessageBoxImage.Error);
                }

                if (task.Result is List<SimpleIncident> list)
                {
                    if (Incidents.Count > 0)
                    {
                        SelectedItem = null;
                        Incidents.Clear();
                    }

                    if (list.Count > 0)
                    {
                        Incidents.AddRange(list);
                        SelectedItem = list[0];
                    }
                }
            }
            catch (Exception e)
            {
                DialogService.ShowMessageDialog("Data request failed due to an exception, please upgrade the software", $"{e.GetType().Name}", System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
                base.ExportToExcel.RaiseCanExecuteChanged();
            }
        }





        private bool IsItemSelected(object arg)
        {
            return SelectedItem is not null;
        }
        private void PerformFireWallAction(SimpleIncident incident)
        {

            var p = new DialogParameters
            {
                { QuickActionViewModel.OBJECT, incident },

            };
            DialogService.ShowDialog(DialogNames.QUICKACTION, parameters: p, callback);
            void callback(IDialogResult obj)
            {

            }
        }

    }
}
