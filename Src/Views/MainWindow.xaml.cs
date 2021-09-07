using Syncfusion.Windows.Shared;
using System;
using System.Windows;

namespace Desktop.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow :ChromelessWindow//Syncfusion.Windows.Tools.Controls.RibbonWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            this.StateChanged += MainWindow_StateChanged;
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                //this.Width = SystemParameters.PrimaryScreenWidth;
                //this.Height = SystemParameters.PrimaryScreenHeight;
                this.Width = SystemParameters.VirtualScreenWidth;
                this.Height = SystemParameters.VirtualScreenHeight;
                //this.Width = SystemParameters.WorkArea.Width                            
                //this.Height =SystemParameters.WorkArea.Height;
            }
        }
    }
    
}
