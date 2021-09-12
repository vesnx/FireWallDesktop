// ***********************************************************************
// Assembly         : Desktop
// Author           : Walter Verhoeven
// Created          : 07-12-2021
//
// Last Modified By : Walter Verhoeven
// Last Modified On : 08-10-2021
// ***********************************************************************
// <copyright file="AbuseReportBuilderViewModel.cs" company="VESNX AG">
//     © 2021 Walter Verhoeven, all rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
using Desktop.Core.Dialogs;
using Desktop.Infrastructure;
using Desktop.Model;
using Desktop.Reporting;
using Desktop.Services.Interfaces;
using log4net;
using Microsoft.Win32;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Events;
using Prism.Services.Dialogs;
using System;
using System.IO;
using System.Threading.Tasks;
using Walter.BOM;

namespace Desktop.Dialogs
{
    public class AbuseReportBuilderViewModel : DialogBase
    {
        internal const string COMMAND = "D";
        private readonly IReporter _reporter;
        private readonly ILog _log;
        private AbuseReportCommand _filter;
        int _filterValue;

        private SimpleAbuseReport _report;
        private bool _portBasedAttacks;
        private bool _webBasedAttacks;


        private AbuseReportSettings _reportSettings = new();
        private bool _makeWebLinkEnabled = false;
        internal static string FILE = "FILE";
        private bool _showGeneratePdf;

        public AbuseReportBuilderViewModel(IDialogService dialogService, IDataService service, IEventAggregator eventAggregator, IReporter reporter, ILog log)
        : base(dialogService, service, eventAggregator)
        {
            ExportToDisk = new DelegateCommand(async () => await PerformExportToDisk(), CanGenerate);
            GeneratePDF = new DelegateCommand(async () => await PerformGeneratePDF(), CanGenerate);
            PreviewPdf = new DelegateCommand(async () => await PerformPreviewPdf(), CanGenerate);
            GenerateTXT = new DelegateCommand(async () => await PerformGenerateTXT(), CanGenerate);
            RefreshNow = new DelegateCommand(PerformRefresh, CanRefresh);
            _reporter = reporter;
            _log = log;
            _log.LogCreated();

            ShowGeneratePdf = RunTime.SelectedFireWall.User.Role.HasFlag(AdminAccess.Guest) == false;
#if DEBUG
            //do not override & hide PDF button if generated in release mode and the user logged in as Guest 
            ShowGeneratePdf = true;
#endif

        }

        public AbuseReportCommand Filter
        {
            get => _filter;
            set
            {
                if (SetProperty(ref _filter, value))
                {
                    _filterValue = value.GetHashCode();
                }
            }
        }
        public bool MakeWebLinkEnabled { get => _makeWebLinkEnabled; set => SetProperty(ref _makeWebLinkEnabled, value); }
        public bool ShowGeneratePdf { get => _showGeneratePdf; set => SetProperty(ref _showGeneratePdf, value); }
        //public ObservableCollection<Resource> ResourceCollection
        //{
        //    get;
        //    set;
        //} = new();




        public DelegateCommand GeneratePDF { get; }
        public DelegateCommand ExportToDisk { get; }
        public DelegateCommand GenerateTXT { get; }



        public bool PortBasedAttacks
        {
            get => _portBasedAttacks;
            set
            {
                if (SetProperty(ref _portBasedAttacks, value))
                {
                    _filter.PortBasedAttacks = value;

                    RefreshNow.RaiseCanExecuteChanged();
                }
            }
        }

        public DelegateCommand PreviewPdf { get; }

        public DelegateCommand RefreshNow { get; }

        public SimpleAbuseReport Report
        {
            get => _report;
            set
            {
                try
                {
                    SetProperty(ref _report, value);
                }
                catch (Exception ex)
                {
                    _log.LogException(ex);
                }
            }

        }

        public bool WebBasedAttacks
        {
            get => _webBasedAttacks;
            set
            {
                if (SetProperty(ref _webBasedAttacks, value))
                {
                    _filter.WebRequests = value;
                    RefreshNow.RaiseCanExecuteChanged();
                }
            }
        }

        public override void OnDialogOpened(IDialogParameters parameters)
        {
            _log.LogDialogOpened(parameters);

            base.LoadingProgress = "Getting abuse...";
            IsBusy = true;
            if (parameters.ContainsKey(COMMAND))
            {
                Filter = parameters.GetValue<AbuseReportCommand>(COMMAND);
                _log.LogInfo($"Load Abuse from filter object {Filter.DataSource} {Filter.Payload} {Filter.Value}");
                WebBasedAttacks = Filter.WebRequests;
                PortBasedAttacks = Filter.PortBasedAttacks;
                Filter.PropertyChanged += Filter_PropertyChanged;
                PerformRefresh();
            }
            else
            {
                _log.LogInfo("Load from file passed via command line");
                var args = Environment.GetCommandLineArgs();
                foreach (var arg in args)
                {

                    if (!string.IsNullOrEmpty(arg) && File.Exists(arg) && arg.EndsWith(".abuse", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            var json = Walter.Cypher.Crypto.Decipher(File.ReadAllText(arg), RunTime.SelectedFireWall.Domain.DnsSafeHost.AsShaHash(HashMethod.SHA1));

                            var data = SimpleAbuseReport.Parse(json);

                            this.Filter = new AbuseReportCommand();

                            this.Filter.DataSource = AbuseReportCommand.Filter.ISPRangeId;
                            this.Filter.Value = data.RangeId;
                            this.Filter.BreadCrumbs = false;
                            this.Filter.From = data.Audit.From;
                            this.Filter.Till = data.Audit.Till;
                            this.Filter.PortBasedAttacks = data.Audit.PortBasedAttacks > 0;
                            this.Filter.WebRequests = data.Audit.WebBasedAttacks > 0;
                            data.Audit.UpdateCount();
                            this.Report = data;

                        }
                        catch (Exception e)
                        {
                            _log.LogException(e);
                            DialogService.ShowMessageDialog("Could not read abuse report, please ensure you are using the correct domain", "Read Abuse data Failed", System.Windows.MessageBoxImage.Error);
                        }

                    }
                }
                IsBusy = false;
                PreviewPdf.RaiseCanExecuteChanged();
                GeneratePDF.RaiseCanExecuteChanged();

            }
        }

        private bool CanGenerate()
        {


            var result = Report is not null
                && IsBusy == false
                && Report.Audit.ReportedEntries > 0;
            MakeWebLinkEnabled = result && _reportSettings.CanMakeWebLink;
            return result;
        }

        private bool CanRefresh()
        {
            return IsBusy == false && _filterValue != _filter.GetHashCode();
        }

        private void Filter_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            RefreshNow.RaiseCanExecuteChanged();
            GeneratePDF.RaiseCanExecuteChanged();
            PreviewPdf.RaiseCanExecuteChanged();
            GenerateTXT.RaiseCanExecuteChanged();
        }

        public AbuseReportSettings ReportSettings { get => _reportSettings; }


        private async Task PerformExportToDisk()
        {
            var dlg = new SaveFileDialog();
            dlg.Title = "Export Abuse report";
            dlg.Filter = "Native abuse data (secure)|*.abuse|JSON text file |*.json|XML text files |*.xml";
            if (RunTime.SelectedFireWall.TryGetSettings(RegistrySettings.ExportAbuseReportPath, out var initialDir))
            {
                dlg.InitialDirectory = initialDir;
            }
            dlg.FileName = $"{DateTime.Now:yyyyMMdd}{Report.RangeName}";

            if (dlg.ShowDialog() == false)
                return;


            var file = new FileInfo(dlg.FileName);
            if (dlg.FilterIndex == 1 && !string.Equals(file.Extension, ".abuse"))
            {
                file.FullName.Replace(file.Extension, ".xml");
            }
            else if (dlg.FilterIndex == 2 && !string.Equals(file.Extension, ".json"))
            {
                file.FullName.Replace(file.Extension, ".json");
            }
            else if (dlg.FilterIndex == 3 && !string.Equals(file.Extension, ".xml"))
            {
                file.FullName.Replace(file.Extension, ".xml");
            }

            if (!file.Directory.Exists)
            {
                file.Directory.Create();
            }

            switch (file.Extension)
            {
                case ".json":
                    await File.WriteAllTextAsync(path: file.FullName, contents: JsonConvert.SerializeObject(Report, RunTime.JsonSettings), PageTokenSource.Token);
                    break;

                case ".xml":
                    System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(Report.GetType());
                    FileStream xmlFile = System.IO.File.Create(file.FullName);
                    x.Serialize(xmlFile, Report);
                    await xmlFile.FlushAsync(PageTokenSource.Token);
                    xmlFile.Dispose();
                    break;
                default:
                    var data = JsonConvert.SerializeObject(Report, RunTime.JsonSettings);
                    var bytes = Walter.Cypher.Crypto.Cipher(data, RunTime.SelectedFireWall.Domain.DnsSafeHost.AsShaHash(HashMethod.SHA1));
                    await File.WriteAllTextAsync(file.FullName, bytes, PageTokenSource.Token);
                    break;
            }

            if (!string.IsNullOrEmpty(file.DirectoryName))
            {
                RunTime.SelectedFireWall.SaveSettings(RegistrySettings.ExportAbuseReportPath, file.DirectoryName);
            }

        }

        private async Task PerformGeneratePDF()
        {

            ReportSettings.SaveSettings();

            var dlg = new SaveFileDialog();
            dlg.Title = "Save Abuse report";
            dlg.Filter = "Adobe PDF|*.pdf";
            dlg.FileName = string.Concat(Report.RangeName ,"_",_filter.From.Year,"_",_filter.From.DayOfYear,"_",_filter.Till.DayOfYear, ".pdf");

            if (dlg.ShowDialog() == true)
            {
                IsBusy = true;
                LoadingProgress = $"Generating {dlg.FileName}";

                await Task.Run(() => _reporter.GeneratePDF(Report, Filter, ReportSettings, new FileInfo(dlg.FileName)), PageTokenSource.Token);

                if (File.Exists(dlg.FileName) && Report.RangeId!=0  && ReportSettings.CanMakeWebLink && ReportSettings.MakeWebLink)
                {
                    LoadingProgress = $"uploading {dlg.FileName} to {RunTime.SelectedFireWall.DisplayName} for ENISA & SOC processing and archiving";
                    using (var reader = File.Open(dlg.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        var pdf = new byte[reader.Length];
                        reader.Read(pdf, 0, pdf.Length);


                        await DataService.StoreAbuseReport(Report,ReportSettings, pdf, PageTokenSource?.Token ?? default);
                    }
                }


                if (base.DialogService.ShowMessageDialog($"Would you like to open {dlg.FileName}?"
                                                        , "Preview"
                                                        , System.Windows.MessageBoxImage.Question
                                                        , System.Windows.MessageBoxButton.YesNo) == ButtonResult.Yes)
                {
                    using System.Diagnostics.Process process = new System.Diagnostics.Process();
                    process.StartInfo = new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true };
                    process.Start();

                }




            }
            IsBusy = false;



        }

        private async Task PerformGenerateTXT()
        {
            var dlg = new SaveFileDialog();
            dlg.Title = "Save Abuse report";
            dlg.Filter = "Text file|*.txt";

            if (dlg.ShowDialog() == true)
            {
                LoadingProgress = $"Generating {dlg.FileName}";

                await Task.Delay(1);

                using var pdf = _reporter.GeneratePDF(Report, Filter, ReportSettings);
                Syncfusion.Windows.PdfViewer.PdfDocumentView pdfDocumentView = new();
                pdfDocumentView.Load(pdf);
                Syncfusion.Pdf.TextLines textLines = new();

                for (int i = 0; i < pdfDocumentView.PageCount; i++)
                {

                    System.IO.File.AppendAllText(dlg.FileName, pdfDocumentView.ExtractText(i, out textLines));

                }
            }
        }

        private async Task PerformPreviewPdf()
        {
            ReportSettings.SaveSettings();

            base.IsBusy = true;
            LoadingProgress = "Generating PDF";
            await Task.Delay(100);


            var file = new FileInfo(Path.Combine(Path.GetTempPath(), $"{DateTime.Now.Ticks}.pdf"));



            try
            {
                await Task.Run(() => _reporter.GeneratePDF(Report, Filter, ReportSettings, file, false), PageTokenSource.Token);
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                DialogService.ShowMessageDialog("Could not generate report due to a data compatibility issue, please update the software to match the firewall", "Failed to generate");
            }
            LoadingProgress = "Loading Viewer";

            if (!file.Exists)
            {
                base.IsBusy = false;
                DialogService.ShowMessageDialog("Could find the generated report, please update the software", "Failed to generate");
                return;
            }



            var p = new DialogParameters()
                {
                    { PdfViewerViewModel.PDF,file}
                };


            base.DialogService.ShowDialog(DialogNames.PDF_VIEWER, p, callback);
            void callback(IDialogResult obj)
            {
                if (file.Exists)
                {
                    file.Delete();
                }
            }
            IsBusy = false;




        }



        private void PerformRefresh()
        {
            _log.LogInfo("calling data");

            LoadingProgress = "Requesting...";
            IsBusy = true;
            DataService.GetAbuseReport(Filter, PageTokenSource.Token).ContinueWith(RenderScreen, PageTokenSource.Token);
        }
        private void RenderScreen(Task<SimpleAbuseReport> task)
        {
            if (InvokeRequired())
            {
                _log.LogInfo("Calling method on UI thread");
                try
                {
                    Invoke(() => RenderScreen(task));
                }
                catch (Exception ex)
                {
                    if (task.IsCanceled)
                    {
                        _log.Warn("Task was canceled before completing");
                    }
                    else
                    {
                        _log.LogException(ex);
                    }
                }
                return;
            }

            _log.LogInfo($"processing task with status {task.Status}");

            if (task.Exception != null)
            {
                _log.LogException($"{task.Exception.Message}", task.Exception.InnerException);
            }

            _filterValue = _filter.GetHashCode();
            LoadingProgress = "Populating...";
            try
            {
                _log.LogInfo($"Processed data UI binding abuse data {task.Result}");

                if (task.Result is SimpleAbuseReport report)
                {
                    if (Report is null)
                    {

                        Report = report;
                    }
                    else
                    {
                        report.Update(report.Audit);
                    }
                    if (ReportSettings.MakeWebLink)
                    {
                        ReportSettings.MakeWebLink = Report.RangeId != 0;
                    }
                }
                else
                {
                    throw new ApplicationDataIncompatibleException<SimpleAbuseReport>(task);
                }

            }
            catch (ApplicationDataIncompatibleException<SimpleAbuseReport> ex)
            {
                _log.LogException(ex);
                base.DialogService.ShowMessageDialog($"Could not load abuse report as {RunTime.SelectedFireWall.DisplayName} did not return one.<br/> Make sure the desktop is compatible with the server", "No data returned", System.Windows.MessageBoxImage.Error);
            }
            catch (Exception e)
            {
                _log.LogException(e);
                base.DialogService.ShowErrorDlg(e);
            }
            finally
            {
                IsBusy = false;
                GeneratePDF.RaiseCanExecuteChanged();
                PreviewPdf.RaiseCanExecuteChanged();
                RefreshNow.RaiseCanExecuteChanged();
                GenerateTXT.RaiseCanExecuteChanged();
                ExportToDisk.RaiseCanExecuteChanged();
            }



        }
    }
}
