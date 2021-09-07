using Desktop.Infrastructure;
using Desktop.Model.Desktop;
using System.Diagnostics;
using System.Windows.Controls;

namespace Desktop.Dialogs
{
    /// <summary>
    /// Interaction logic for VisitorsDialog
    /// </summary>
    public partial class VisitorsDialog : UserControl
    {
        public VisitorsDialog()
        {
            //BindingErrorTraceListener.SetTrace();
            InitializeComponent();
        }

        private void OnCellNavigate(object sender, Syncfusion.UI.Xaml.Grid.CurrentCellRequestNavigateEventArgs e)
        {
            if (e.RowData is SimpleRequest request)
            {
                try
                {
                    ProcessStartInfo sI = new ProcessStartInfo(request.URL.AbsoluteUri) { UseShellExecute = true };
                    Process.Start(sI);
                }
                catch { }
            }
        }

        private void PropertyGrid_SelectedPropertyItemChanged(System.Windows.DependencyObject d, 
            System.Windows.DependencyPropertyChangedEventArgs e)
        {
            pg.DescriptionPanelVisibility = System.Windows.Visibility.Visible;
        }
    }
}
