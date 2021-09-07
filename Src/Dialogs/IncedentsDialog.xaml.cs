using System.Windows.Controls;

namespace Desktop.Dialogs
{
    /// <summary>
    /// Interaction logic for IncedentsDialog
    /// </summary>
    public partial class IncedentsDialog : UserControl
    {
        public IncedentsDialog()
        {
            InitializeComponent();
        }

        private void PropertyGrid_SelectedPropertyItemChanged(System.Windows.DependencyObject d,
            System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if(pg.SelectedPropertyItem is not null)
                pg.DescriptionPanelVisibility = System.Windows.Visibility.Visible;
            else
                pg.DescriptionPanelVisibility = System.Windows.Visibility.Collapsed;
        }
    }
}
