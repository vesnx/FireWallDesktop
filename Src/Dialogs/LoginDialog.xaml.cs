using Desktop.Core.Events;
using Prism.Events;
using System.Windows;

namespace Desktop.Dialogs
{

    /// <summary>
    /// Interaction logic for LoginDialog.xaml
    /// </summary>
    public partial class LoginDialog : Window
    {
        public LoginDialog(IEventAggregator eventAggregator)
        {
            InitializeComponent();
            eventAggregator.GetEvent<CloseDialogWindowEvent>().Subscribe(OnCloseWindow);
        }

        private void OnCloseWindow(DialogStatus obj)
        {
            if (DialogResult != obj.DialogResult)
            {
                try
                {
                    DialogResult = obj.DialogResult;
                }
                catch
                {
                }
            }

        }
    }
}
