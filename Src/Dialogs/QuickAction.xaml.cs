using System.Windows.Controls;

namespace Desktop.Dialogs
{
    /// <summary>
    /// Interaction logic for QuickAction
    /// </summary>
    public partial class QuickAction : UserControl
    {
        public QuickAction()
        {
            InitializeComponent();
        }

        private void PropertyGrid_SelectedPropertyItemChanged(System.Windows.DependencyObject d,
            System.Windows.DependencyPropertyChangedEventArgs e)
        {
            pg.DescriptionPanelVisibility = System.Windows.Visibility.Visible;
        }

        private void OnPropertyGridCollectionOpened(object sender, Syncfusion.Windows.PropertyGrid.CollectionEditorOpeningEventArgs e)
        {
            e.IsReadonly = true;
            
        }
    }
}
