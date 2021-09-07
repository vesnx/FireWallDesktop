using System;
using System.Globalization;
using System.Windows.Data;
namespace Desktop.Converters
{

    public class ToParentSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (int.TryParse(value.ToString(), out int size))
            {
                if (size > 0)
                {
                    return size - 1;
                }
            }

            return 1740;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
