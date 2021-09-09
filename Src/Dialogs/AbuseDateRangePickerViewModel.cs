using Desktop.Model;
using Desktop.Services.Interfaces;
using log4net;
using Prism.Commands;
using Prism.Events;
using Prism.Services.Dialogs;
using System;
using Walter.BOM;

namespace Desktop.Dialogs
{
    public class AbuseDateRangePickerViewModel : DialogBase, IDialogAware
    {
        internal const string Command = "Args";
        private readonly ILog _log;
        AbuseReportCommand _filter;
        public AbuseDateRangePickerViewModel(IDialogService dialogService, IDataService service, IEventAggregator eventAggregator, ILog log)
        : base(dialogService, service, eventAggregator)
        {
            _log = log;
            _log.LogCreated();
            DlgOkCommand = new DelegateCommand<string>(OnOkButton, CanSubmit);

        }

        private bool CanSubmit(string _)
        {
            if (_filter is null)
            {
                return false;
            }
            return (_filter.PortBasedAttacks || _filter.WebRequests) && (Till-From).TotalSeconds>0;
        }

        private void OnOkButton(string args)
        {
            var p = new DialogParameters();
            p.Add(Command, _filter);

            base.Fire_RequestClose(ButtonResult.OK, p);
        }

        public Prism.Commands.DelegateCommand<string> DlgOkCommand { get; }

        bool _portBasedAttacks;
        public bool PortBasedAttacks
        {
            get => _portBasedAttacks;
            set
            {
                if (SetProperty(ref _portBasedAttacks, value))
                {
                    _filter.PortBasedAttacks = value;

                    DlgOkCommand.RaiseCanExecuteChanged();
                }
            }
        }
        bool _webBasedAttacks;
        public bool WebBasedAttacks
        {
            get => _webBasedAttacks;
            set
            {
                if (SetProperty(ref _webBasedAttacks, value))
                {
                    _filter.WebRequests = value;
                     DlgOkCommand.RaiseCanExecuteChanged();
                }
            }
        }




        public DateTime From { get=>_filter?.From??DateTime.MinValue;
            set {

                _filter.From = value;
                RaisePropertyChanged(nameof(From));

            }
        }
        public DateTime Till { get=>_filter?.Till??DateTime.MinValue; 
            set{
                _filter.Till = value;
                RaisePropertyChanged(nameof(Till));
            } 
        }

       
        public override void OnDialogOpened(IDialogParameters parameters)
        {
            Title = "Select the data for the abuse report";

            if (parameters.TryGetValue<AbuseReportCommand>(Command, out var filter))
            {
                _filter = filter;
  
                PortBasedAttacks = _filter.PortBasedAttacks;
                WebBasedAttacks = _filter.WebRequests;
                From = _filter.From;
                Till = _filter.Till;
            }
        }




    }
}
