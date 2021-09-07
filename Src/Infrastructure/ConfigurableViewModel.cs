using Desktop.Core;
using Desktop.Model.Infrastructure;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace Desktop.Infrastructure
{
    public static class BrowserBehavior
    {
        public static readonly DependencyProperty HtmlProperty = DependencyProperty.RegisterAttached(
            "Html",
            typeof(string),
            typeof(BrowserBehavior),
            new FrameworkPropertyMetadata(OnHtmlChanged));

        [AttachedPropertyBrowsableForType(typeof(WebBrowser))]
        public static string GetHtml(WebBrowser d)
        {
            return (string)d.GetValue(HtmlProperty);
        }

        public static void SetHtml(WebBrowser d, string value)
        {
            d.SetValue(HtmlProperty, value);
        }

        static void OnHtmlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

            if (d is WebBrowser wb)
            {
                wb.NavigateToString(e.NewValue as string);
            }
        }
    }

    public class ConfigurableViewModel : BindableBase
    {
        private readonly BasicUserPrefferences _prefferences;
        private readonly IRegionManager _regionManager;

        public ConfigurableViewModel(BasicUserPrefferences prefferences, IRegionManager regionManager)
        {
            _prefferences = prefferences;
            _regionManager = regionManager;
            ChangeTimeRangeScale = new DelegateCommand<string>(UpdateTimeScalePrefferences);

        }

        public ObservableCollection<LoggedError> Errors { get; } = new();
        
        protected void LogException(Exception e)
        {
            Errors.Add(new(titel: "Stats Counter", exception: e));
        }

        private void UpdateTimeScalePrefferences(string obj)
        {
            if (Enum.TryParse<TimeScale>(obj, out var value))
            {
                _prefferences.TimeScale = value;
                RaisePropertyChanged("TimeScale");
            }
        }

        public DelegateCommand<string> ChangeTimeRangeScale { get; private set; }

        protected bool InvokeRequired() => Thread.CurrentThread != Application.Current.Dispatcher.Thread;

        protected  void InvokeIfNecessary(Action action)
        {
            if (InvokeRequired())
            {
                Application.Current.Dispatcher.Invoke(action);                
            }
            else
            {
                action();
            }
        }

        protected void PerformNavigation(string viewName)
        {
            //_regionManager.Regions[RegionNames.ContentRegion]
            //                .NavigationService.RequestNavigate(viewName);

            _regionManager.RequestNavigate(RegionNames.ContentRegion, viewName);
        }

        protected void PerformNavigation(string viewName, NavigationParameters parameters)
        {
            //_regionManager.Regions[RegionNames.ContentRegion]
            //                .NavigationService.RequestNavigate(viewName);

            _regionManager.RequestNavigate(RegionNames.ContentRegion, viewName,parameters);
        }


    }
}
