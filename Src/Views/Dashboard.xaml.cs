using Microsoft.Win32;
using Syncfusion.XlsIO;
using System.IO;
using System.Windows.Controls;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.Grid.Converter;
using Syncfusion.UI.Xaml.CellGrid.Converter;
using System.Windows;
using Desktop.Model.Desktop;
using System.Linq;

namespace Desktop.Views
{
    /// <summary>
    /// Interaction logic for Desktop
    /// </summary>
    public partial class Dashboard : UserControl
    {
        public Dashboard()
        {
            InitializeComponent();
        }
        private void sfISPDataGidTooltipOpening(object sender, GridCellToolTipOpeningEventArgs e)
        {

            //only check the column Header Row 
            if (e.RowColumnIndex.RowIndex == 0)
            {
                //get the underline model type 
                var enumType = typeof(SimpleISP);
                //get the field name for column 
                var memberInfos = enumType.GetMember(e.Column.MappingName);
                var namedArguments = memberInfos[0].CustomAttributes.FirstOrDefault().NamedArguments;
                string description = string.Empty;

                foreach (var s in namedArguments)
                {
                    if (s.MemberName == "Description")
                    {
                        description = s.TypedValue.Value.ToString();
                        //set the Description to tooltip content 
                        e.ToolTip.Content = description;
                        break;
                    }
                }
            }
        } 
        private void ButtonAdv_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog
            {
                FilterIndex = 2,
                Filter = "Excel 97 to 2003 Files(*.xls)|*.xls|Excel 2007 to 2010 Files(*.xlsx)|*.xlsx|Excel 2013 File(*.xlsx)|*.xlsx"
            };
            var options = new ExcelExportingOptions() { ExcelVersion= ExcelVersion.Excel2013 };
            
            
            using var excelEngine = ispGrid.ExportToExcel(ispGrid.View, excelExportingOptions:options);
            var workBook = excelEngine.Excel.Workbooks[0];

            if (sfd.ShowDialog() == true)
            {
                using (Stream stream = sfd.OpenFile())
                {

                    if (sfd.FilterIndex == 1)
                    {
                        workBook.Version = ExcelVersion.Excel97to2003;
                    }
                    else if (sfd.FilterIndex == 2)
                    {
                        workBook.Version = ExcelVersion.Excel2010;
                    }
                    else
                    {
                        workBook.Version = ExcelVersion.Excel2013;
                    }


                    workBook.SaveAs(stream);
                }

            }
        }

        private void sfAutoGenerateColumns(object sender, AutoGeneratingColumnArgs e)
        {
            e.Column.ShowHeaderToolTip = true;
        }
    }
}
