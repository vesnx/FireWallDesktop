using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Desktop.Dialogs
{
    public class ErrorsDialigViewModel : BindableBase, IDialogAware
    { 
        internal const string OBJECT = "Data";
        private string _title;

        public ErrorsDialigViewModel()
        {

            DlgCommand = new DelegateCommand(CloseDialogOK);
        }
        private void CloseDialogOK()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
        }


        public string Title { get => _title; set =>SetProperty(ref _title , value); }

        public event Action<IDialogResult> RequestClose;
        public DelegateCommand DlgCommand { get; private set; }
        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
            
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            
        }
    }
}
