using Desktop.Infrastructure;
using Desktop.Model;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Desktop.Converters
{
    public class IPAddressAbuseToTimeLineConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {

            if (value is IPAddressAbuse data)
            {
                var coll = data.GetTimeLine();
                return coll;
            }

            return new System.Collections.ObjectModel.ObservableCollection<Syncfusion.Windows.Controls.Gantt.TaskDetails>();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return parameter;
        }
    }
}
