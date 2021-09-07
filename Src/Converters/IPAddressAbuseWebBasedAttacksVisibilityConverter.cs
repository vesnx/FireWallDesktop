using Desktop.Model;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Desktop.Converters
{
    public class IPAddressAbuseWebBasedAttacksVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {

            if (value is IPAddressAbuse data)
            {

                return data.WebBasedAttacksCount > 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return parameter;
        }
    }
}
