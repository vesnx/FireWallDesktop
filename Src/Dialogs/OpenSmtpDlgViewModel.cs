using System;
using System.Collections.Generic;
using System.Linq;
using Desktop.Services.Interfaces;
using log4net;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace Desktop.Dialogs
{
    public class OpenSmtpDlgViewModel : DialogBase, IDialogAware
    {
        public OpenSmtpDlgViewModel(IDialogService dialogService, IDataService service, IEventAggregator eventAggregator, ILog log)
            : base(dialogService, service, eventAggregator)
        {

        }



        public override void OnDialogOpened(IDialogParameters parameters)
        {
           
        }
    }
}
