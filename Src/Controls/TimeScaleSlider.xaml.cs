using Desktop.Model.Infrastructure;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Desktop.Core.Controlls
{
    public class NameValueLabel
    {
        public int Value { get; set; }
        public string Name { get; set; }
    }

    /// <summary>
    /// Interaction logic for TimeScaleSlider.xaml
    /// </summary>
    public partial class TimeScaleSlider : UserControl, INotifyPropertyChanged
    {
        private int _maximumValue;

        public TimeScaleSlider()
        {

            InitializeComponent();

            var values = Enum.GetValues<TimeScale>();
            Labels.AddRange(values.Select(s => new NameValueLabel() { Name = s.ToString(), Value = (int)s }));
            MaximumValue = values.Max(m => (int)m);


        }


        public int SelectedValue
        {
            get { return (int)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Value.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("SelectedValue", typeof(int), typeof(TimeScaleSlider), new PropertyMetadata(0));



        public ObservableCollection<NameValueLabel> Labels { get; private set; } = new ();
        public int MaximumValue
        {
            get => _maximumValue;
            set
            {
                if (_maximumValue != value)
                {
                    _maximumValue = value;
                    RaisePropertyChange();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChange([CallerMemberName] string propertyname = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));
        }


    }
}
