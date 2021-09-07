using Desktop.Model;
using Syncfusion.Windows.PdfViewer;
using System;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Desktop.Dialogs
{
    /// <summary>
    /// Interaction logic for PdfViewer
    /// </summary>
    public partial class PdfViewer : UserControl
    {
        public PdfViewer()
        {
            InitializeComponent();
            pdfViewer.ToolbarSettings.ShowFileTools = RunTime.SelectedFireWall.User.Role.HasFlag(AdminAccess.Guest) == false;

        }
        //private void HideToolbars(object sender, EventArgs e)
        //{
        //    if (RunTime.SelectedFireWall.User.Role.HasFlag(AdminAccess.Guest) == false)
        //    {
        //        return;
        //    }


        //    if (sender is PdfViewerControl PdfViewer)
        //    {
        //        //Get the instance of the toolbar using its template name.
        //        DocumentToolbar toolbar = PdfViewer.Template.FindName("PART_Toolbar", PdfViewer) as DocumentToolbar;

        //        //Get the instance of the file menu button using its template name.
        //        ToggleButton FileButton = (ToggleButton)toolbar.Template.FindName("PART_FileToggleButton", toolbar);


        //        //Get the instance of the file menu button context menu and the item collection.
        //        ContextMenu FileContextMenu = FileButton.ContextMenu;
        //        foreach (MenuItem FileMenuItem in FileContextMenu.Items)
        //        {
        //            //Get the instance of the open menu item  and save menu item using its template name and disable its visibility.
        //            if (!string.Equals(FileMenuItem.Name, "PART_OpenMenuItem", StringComparison.Ordinal) && FileMenuItem.Visibility != System.Windows.Visibility.Collapsed)
        //            {
        //                FileMenuItem.Visibility = System.Windows.Visibility.Collapsed;
        //            }

        //        }
        //    }
        //}
    }
}
