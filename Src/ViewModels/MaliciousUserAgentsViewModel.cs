using Desktop.Infrastructure;
using Desktop.Model.Infrastructure;
using Desktop.Model.UserAgent;
using Prism.Regions;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Desktop.ViewModels
{
    public abstract class BaseFilter
    {
        public DateTime From { get; set; }
        public DateTime Till { get; set; }
    }

    public class UserAgentFilter:BaseFilter
    { 
    }
    public class MaliciousUserAgentsViewModel : ConfigurableViewModel,INavigationAware ,INotifyPropertyChanged
    {

        internal const string TimeTicsParameter = "TT";
        internal const string TimeScaleParameter = "TS";
        public MaliciousUserAgentsViewModel(UserPrefference userPrefference, IRegionManager regionManager)
            : base(userPrefference.MaliciousUserAgentsProperties, regionManager)
        {
            Filter = new UserAgentFilter()
            {
                From = DateTime.UtcNow.Subtract(userPrefference.MaliciousUserAgentsProperties.DataRange),
                Till = DateTime.UtcNow
            };

        }
        public UserAgentFilter Filter { get; private set; }

        private ObservableCollection<object> _selectedItems;
        public ObservableCollection<object> SelectedItems
        {
            get
            {
                return _selectedItems;
            }
            set
            {
                _selectedItems = value;
                RaisePropertyChanged("SelectedItems");
            }
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {

        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            if (navigationContext.Parameters.TryGetValue(TimeTicsParameter, out int ticks)
                &&navigationContext.Parameters.TryGetValue(TimeScaleParameter, out TimeScale scale))
            {
                Filter.From= DateTime.UtcNow.Subtract(scale.GetTimeSpan(ticks));
                Filter.Till = DateTime.UtcNow;
            }
        }


        public ObservableCollection<UserAgent> UserAgents { get; } = new();
    }
}
