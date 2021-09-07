using Desktop.Model;
using Desktop.Model.Desktop;
using Desktop.Model.Navigation;
using Desktop.Services.Interfaces;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Desktop.Dialogs
{
    public class RequestFlowViewModel : DialogBase
    {
        internal static string OBJECT = "OBJECT";
        internal static string SCOPE = "SCOPE";
        private SimpleVisitSession _visitSession;
        private TimeSpan _time;

        public RequestFlowViewModel(IDialogService dialogService, IDataService service, IEventAggregator eventAggregator)
: base(dialogService, service, eventAggregator)
        {
            FireWall = RunTime.SelectedFireWall;
        }

        public ObservableCollection<SimpleBreadCrumbCollection> BreadCrumbs { get; } = new();
        public FireWall FireWall { get; }
        public SimpleVisitSession VisitSession { get => _visitSession; private set =>SetProperty(ref _visitSession , value); }
        public ObservableCollection<SimpleRequest> Data { get; } = new();
        public override void OnDialogOpened(IDialogParameters parameters)
        {
            VisitSession = parameters.GetValue<SimpleVisitSession>(OBJECT);
            _time= parameters.GetValue<TimeSpan>(SCOPE);
            HashSet<long> _cidr = new();
            Title= $"{VisitSession.IPAddress} ";
            
            IsBusy = true;


            for (int i = 0; i < VisitSession.Activity.Count; i++)
            {
                if (VisitSession.Activity[i].Tag is SimpleRequest request)
                {
                    Data.Add(request);
                    _cidr.Add(request.CIDR);
                }
            }

            base.LoadingProgress = "requesting user activity diagrams...";
            foreach (var cidr in _cidr)
            {
                DataService.GetBreadCrumbs(_time, cidr, PageTokenSource.Token)
                            .ContinueWith(PopuLateActivityDiagram, PageTokenSource.Token);
            }

        }

        private void PopuLateActivityDiagram(Task<List<SimpleBreadCrumbCollection>> task)
        {
            base.LoadingProgress = "Generating user activity diagrams";
            if (task.Result is List<SimpleBreadCrumbCollection> items)
            {
                InvokeIfNecessary(() => PopuLateActivityDiagram(items));
            }

        }
        private void PopuLateActivityDiagram(List<SimpleBreadCrumbCollection> items)
        {           

            BreadCrumbs.AddRange(items);
            IsBusy = false;
        }

    }
}
