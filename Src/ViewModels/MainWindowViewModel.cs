using Desktop.Core.Events;
using Desktop.Dialogs;
using Desktop.Model;
using Desktop.Model.Events;
using Desktop.Services.Interfaces;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Prism.Commands;
using Syncfusion.SfSkinManager;
using System.Collections.Generic;
using Desktop.Core;
using Prism.Regions;
using System;

namespace Desktop.ViewModels
{
    public class ObservableCollection : ObservableCollection<object> { }
    public class MainWindowViewModelLoginViewModel
    { 
    
    }

    public class MainWindowViewModel : BindableBase
    {
        private string _title = "ASP-WAF Firewall Dashboard";
        private string _theme = RunTime.ThemeName;
        SubscriptionToken _events;
        private int _selectedThemeIndex = 0;
        private string displayName;
        private readonly IContainerProvider _container;
        private readonly IRegionManager _regionManager;

        public MainWindowViewModel(IEventAggregator eventAggregator, IContainerProvider container, IRegionManager regionManager)
        {
            _events = eventAggregator
                     .GetEvent<FireWallStateChangedEvent>()
                     .Subscribe(
                          action: OnFireWallStatusChanged
                         , threadOption: ThreadOption.UIThread
                         , keepSubscriberReferenceAlive: false
                         , filter => filter.NewState != FireWallStates.None && !filter.NewState.HasFlag(FireWallStates.IsConnecting)
                         );


            _container = container;
            _regionManager = regionManager;
            ChangeTheme = new DelegateCommand<string>(PerformChangeTheme);
            NavigateTo = new DelegateCommand<string>(PerformNavigation);
            SignOut = new DelegateCommand(PerformSignOut);

        }

        private void PerformSignOut()
        {

            var auth = _container.Resolve<IAuthenticationService>();
            auth.LogoutAsync(RunTime.SelectedFireWall);
            RunTime.SelectedFireWall.State = FireWallStates.None;
            RunTime.SelectedFireWall.RememberUser = false;
            RunTime.SaveStateToDisk();


            var login = _container.Resolve<LoginDialog>();
            var loginResult = login.ShowDialog();
            if (loginResult == false || RunTime.SelectedFireWall == null)
            {
                Application.Current.Shutdown();
            }

        }

        private void PerformNavigation(string viewName)
        {
            //_regionManager.Regions[RegionNames.ContentRegion]
            //                .NavigationService.RequestNavigate(viewName);

            _regionManager.RequestNavigate(RegionNames.ContentRegion, viewName);
        }


        private void OnFireWallStatusChanged(FireWallStateChangedArgs args)
        {
            if (displayName != args.FireWall.DisplayName)
            {
                DisplayName = args.FireWall.DisplayName;            
                Domain = args.FireWall.Domain;
            }
            if (args.FireWall.User is not null && UserName != args.FireWall.User.UserName)
            {
                UserName = args.FireWall.User.UserName;
                Role = args.FireWall.User.Role.ToString();
            }
            else
            {
                UserName =string.Empty;
                Role = string.Empty;
            }

            State = args.NewState;


            Title =  args.FireWall.User is not null ? $"{args.FireWall.DisplayName}  {args.FireWall.Domain} - User:{args.FireWall.User.UserName} Role:{args.FireWall.User.Role} - {args.FireWall.State}"
               : $"{args.FireWall.DisplayName}/{args.FireWall.Domain} - {args.FireWall.State}";

            //if (!args.NewState.HasFlag(FireWallStates.IsConnected) 
            //    && !args.NewState.HasFlag(FireWallStates.IsConnecting)
            //    && args.OldState.HasFlag(FireWallStates.WasConnected)
            //    )
            //{
            //    args.FireWall.State |= FireWallStates.IsConnecting;
            //    IAuthenticationService _authenticationService = _container.Resolve<IAuthenticationService>();
            //    var result = await _authenticationService.LoginAsync(args.FireWall, args.FireWall.User.UserName, args.FireWall.User.GetPassword());
            //    if (result != System.Net.HttpStatusCode.OK)
            //    {
            //        RunTime.SelectedFireWall = null;
            //        var login = _container.Resolve<LoginDialog>();
            //        var loginResult = login.ShowDialog();
            //        if (loginResult == false || RunTime.SelectedFireWall == null)
            //        {
            //            Application.Current.Shutdown();
            //        }
            //    }
            //}

        }

        public FireWallStates State { get; set; }
        public string UserName { get; set; }
        public string Role { get; set; }
        public Uri Domain { get; set; }
        public string DisplayName { get => displayName; set => displayName = value; }

        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        public List<string> Themes { get; } = new() { ThemeNames.Office2016DarkGray, ThemeNames.Office365 };

        public int SelectedThemeIndex
        {
            get => _selectedThemeIndex;
            set
            {
                SetProperty(ref _selectedThemeIndex, value);
                Theme = Themes[value];
            }
        }

        public string Theme
        {
            get => _theme;
            set => SetProperty(ref _theme, value);
        }

        public DelegateCommand<string> ChangeTheme { get; private set; }
        public DelegateCommand<string> NavigateTo { get; private set; }
        public DelegateCommand SignOut { get; private set; }

        private void PerformChangeTheme(string arg)
        {
            Theme = arg;

        }

    }
}
