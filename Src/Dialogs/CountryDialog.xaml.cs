using System.Windows.Controls;

namespace Desktop.Dialogs
{
    /// <summary>
    /// Interaction logic for CountryDialog
    /// </summary>
    public partial class CountryDialog : UserControl
    {
        public CountryDialog()
        {
            InitializeComponent();
        }

        private void PropertyGrid_SelectedPropertyItemChanged(System.Windows.DependencyObject d, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            pg.DescriptionPanelVisibility = System.Windows.Visibility.Visible;
        }
    }
}
