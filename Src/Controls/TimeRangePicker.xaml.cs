using Desktop.Model.Infrastructure;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Desktop.Core.Controlls
{
    /// <summary>
    /// Interaction logic for TimeRangePicker.xaml
    /// </summary>
    public partial class TimeRangePicker : UserControl,INotifyPropertyChanged
    {
        public TimeRangePicker()
        {
            InitializeComponent();
        }



        public TimeSpan Range
        {
            get 
            {
                return Scale.GetTimeSpan(Value);
            }
    
        }

        // Using a DependencyProperty as the backing store for Range.  This enables animation, styling, binding, etc...
        public static readonly DependencyPropertyKey RangeProperty = DependencyProperty.RegisterReadOnly("Range"
                                                                    , typeof(TimeSpan)
                                                                    , typeof(TimeRangePicker)
                                                                    , new FrameworkPropertyMetadata(default)
                                                                    );




        public int Maximum
        {
            get { return (int)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Maximum.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(int), typeof(TimeRangePicker), new PropertyMetadata(0));



        public int Value
        {
            get { return (int)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Value.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(int), typeof(TimeRangePicker), new PropertyMetadata(0));




        public TimeScale Scale
        {
            get { return (TimeScale)GetValue(ScaleProperty); }
            set { 

                SetValue(ScaleProperty, value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Range)));                
            }
        }

        // Using a DependencyProperty as the backing store for Scale.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ScaleProperty =
            DependencyProperty.Register("Scale", typeof(TimeScale), typeof(TimeRangePicker), new PropertyMetadata(0));

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
