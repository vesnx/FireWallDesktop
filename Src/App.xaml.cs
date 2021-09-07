using Desktop.Authentication;
using Desktop.Core;
using Desktop.Core.Builder;
using Desktop.Core.RegionAdapters;
using Desktop.Dialogs;
using Desktop.Model;
using Desktop.Model.Infrastructure;
using Desktop.Services.Builder;

using Desktop.Reporting.Builder;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using Prism.Services.Dialogs;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Desktop.Core.Dialogs;
using Desktop.Services.Interfaces;
using System.IO;
using Desktop.Infrastructure;
using System.Reflection;
using log4net;

namespace Desktop
{
    class DialogNames
    {
        internal const string VisitStats_DIALOGUE =nameof(VisitorsDialog);
        internal const string IncidentStats_DIALOGUE =nameof(IncedentsDialog);
        internal const string QUICKACTION =nameof(QuickAction);
        internal const string REQUESTFLOW =nameof(RequestFlow);
        internal const string ISPDETAIL_DIALOGUE = nameof(WHOISDialog);
        internal const string TRACERT_DIALOGUE = nameof(Tracert);
        internal const string COUNTRY_DIALOG = nameof(CountryDialog);
        internal const string ERRORS = nameof(ErrorsDialig);
        internal const string ABUSE_REPORT = nameof(AbuseReportBuilder);
         internal const string ABUSE_REPORT_DATE_PICKER = nameof(AbuseDateRangePicker);
        internal const string PDF_VIEWER = nameof(PdfViewer);
    }

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        FileInfo abuseFile=null;
        private static readonly ILog log = LogManager.GetLogger(typeof(App));
        public App()
        {

            RunTime.ThemeName = ThemeNames.Office2016DarkGray;
            RunTime.ThemeKey = ThemeNames.Office2016DarkGrayKey;

        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            log.LogException("Unhandled exception", e.Exception);

            MessageBox.Show("An unhandled exception just occurred: " + e.Exception.Message, "Exception Sample", MessageBoxButton.OK, MessageBoxImage.Warning);
            e.Handled = true;
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            log4net.Config.XmlConfigurator.Configure(new FileInfo("log4net.config"));
            log.Info("        =============  Started Logging  =============        ");

            FileAssociations.EnsureAssociationsSet(new FileAssociation()
            {
                ExecutableFilePath = Assembly.GetExecutingAssembly().GetFileInfo().FullName,
                Extension = ".abuse",
                FileTypeDescription="Native ASP-WAF firewall abuse report",
                ProgId="FireWall_Desktop"
            });

            foreach (var arg in e.Args)
            {
                if (arg.Length > 3 && arg.EndsWith(".abuse", StringComparison.OrdinalIgnoreCase) && File.Exists(arg))
                { 
                    abuseFile= new FileInfo(arg);
                    log.Info($"Started application with abuse file {arg}");
                }
            }
            base.OnStartup(e);

        }
        protected override Window CreateShell()
        {
            return Container.Resolve<Views.MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterDialog<Dialogs.VisitorsDialog, Dialogs.VisitorsDialogViewModel>(DialogNames.VisitStats_DIALOGUE);
            containerRegistry.RegisterDialog<Dialogs.IncedentsDialog, Dialogs.IncedentsDialogViewModel>(DialogNames.IncidentStats_DIALOGUE);
            containerRegistry.RegisterDialog<Dialogs.QuickAction, Dialogs.QuickActionViewModel>(DialogNames.QUICKACTION);
            containerRegistry.RegisterDialog<Dialogs.Tracert, Dialogs.TracertViewModel>(DialogNames.TRACERT_DIALOGUE);
            containerRegistry.RegisterDialog<Dialogs.WHOISDialog, Dialogs.WHOISDialogViewModel>(DialogNames.ISPDETAIL_DIALOGUE);
            containerRegistry.RegisterDialog<Dialogs.CountryDialog, Dialogs.CountryDialogViewModel>(DialogNames.COUNTRY_DIALOG);
            containerRegistry.RegisterDialog<Dialogs.ErrorsDialig, Dialogs.ErrorsDialigViewModel>(DialogNames.ERRORS);
            containerRegistry.RegisterDialog<Dialogs.RequestFlow, Dialogs.RequestFlowViewModel>(DialogNames.REQUESTFLOW);
            containerRegistry.RegisterDialog<Dialogs.AbuseReportBuilder, Dialogs.AbuseReportBuilderViewModel>(DialogNames.ABUSE_REPORT);
            containerRegistry.RegisterDialog<Dialogs.PdfViewer, Dialogs.PdfViewerViewModel>(DialogNames.PDF_VIEWER);
            containerRegistry.RegisterDialog<Dialogs.AbuseDateRangePicker, Dialogs.AbuseDateRangePickerViewModel>(DialogNames.ABUSE_REPORT_DATE_PICKER);

            containerRegistry.RegisterInstance<ILog>(log);
            containerRegistry.RegisterSingleton<UserPrefference>();
            containerRegistry.RegisterForNavigation<Views.Dashboard, ViewModels.DashboardViewModel>();

            containerRegistry.RegisterForNavigation<Views.MaliciousUserAgents, ViewModels.MainWindowViewModel>();
            containerRegistry.RegisterServices()
                             .RegisterDefaultDialogs()
                             .RegisterServicesReporter();

            // containerRegistry.RegisterScoped<StackPanelRegionAdapter>();

        }
        protected override void ConfigureRegionAdapterMappings(RegionAdapterMappings regionAdapterMappings)
        {
            base.ConfigureRegionAdapterMappings(regionAdapterMappings);
            regionAdapterMappings.RegisterMapping(typeof(StackPanel), Container.Resolve<StackPanelRegionAdapter>());
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            base.ConfigureModuleCatalog(moduleCatalog);
            moduleCatalog.AddModule<AuthenticationModule>();
        }

        protected override void OnNavigated(NavigationEventArgs e)
        {
            base.OnNavigated(e);

        }


        protected override void OnExit(ExitEventArgs e)
        {
            if (RunTime.SelectedFireWall is not null)
            {
                var auth = Container.Resolve<IAuthenticationService>();
                auth.LogoutAsync(RunTime.SelectedFireWall);

            }
            log.Info("Closed application");
            log4net.LogManager.Flush(10);
            base.OnExit(e);
        }

        protected override void OnNavigating(NavigatingCancelEventArgs e)
        {
            base.OnNavigating(e);
            log.Info($"Navigated to {e.Uri}");

        }

        protected override async void OnInitialized()
        {
            var con = Container.Resolve<IConnectFileImporterService>();
            
            if (con.HasAutoLogin(out var firewall)
                && !string.IsNullOrEmpty(firewall.User.UserName)
                && firewall.User.HasPassword())
            {
                var auth = Container.Resolve<IAuthenticationService>();
                var result = await auth.LoginAsync(firewall, firewall.User.UserName, firewall.User.GetPassword());
                if (result == System.Net.HttpStatusCode.OK)
                {
                    RunTime.SelectedFireWall = firewall;
                    if (!string.IsNullOrEmpty(firewall.User.SelectedView))
                    {
                        var region = Container.Resolve<IRegionManager>();
                        region.RequestNavigate(RegionNames.ContentRegion, nameof(firewall.User.SelectedView));
                    }
                }
                else
                {
                    if (auth.CoveredByLogin(result))
                    {
                        Application.Current.Shutdown();
                        return;
                    }
                }

            }

            if (RunTime.SelectedFireWall is null || !RunTime.SelectedFireWall.State.HasFlag(FireWallStates.IsConnected))
            {
                if (RunTime.SelectedFireWall is not null)
                {
                    var dlsg = Container.Resolve<IDialogService>();
                    dlsg.ShowMessageDialog($"Login to {RunTime.SelectedFireWall.Domain.DnsSafeHost} failed, please make sure the firewall is on-line", "Auto-login failed");
                }
                
                var login = Container.Resolve<LoginDialog>();
                var result = login.ShowDialog();
                if (!result.HasValue || !result.Value)
                {
                    RunTime.SelectedFireWall = null;
                }
            }

            if (RunTime.SelectedFireWall is null)
            {
                if (!Environment.HasShutdownStarted)
                {
                    Application.Current.Shutdown();
                }

            }
            else
            {
                
                base.OnInitialized();

                if (abuseFile is not null)
                {
                    var dlgService = Container.Resolve<IDialogService>();
                    var p = new DialogParameters
                    {
                        { Dialogs.AbuseReportBuilderViewModel.FILE, abuseFile },

                    };
                    dlgService.ShowDialog(DialogNames.ABUSE_REPORT, parameters: p, callback);
                    void callback(IDialogResult obj)
                    {

                    }
                    Application.Current.Shutdown();

                }
                else
                { 
                    var region = Container.Resolve<IRegionManager>();
                    region.RequestNavigate(RegionNames.ContentRegion, nameof(Views.Dashboard));
                }
            }


        }
    }
}
