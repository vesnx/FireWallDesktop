using Desktop.Model;
using System;
using System.Collections.Generic;
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

namespace Desktop.Controls
{
    /// <summary>
    /// Interaction logic for PortBasedAttacks.xaml
    /// </summary>
    public partial class PortBasedAttacks : UserControl
    {
        public PortBasedAttacks()
        {

            InitializeComponent();

        }
        


        public GroupPortAttack PortAttack
        {
            get { 
                return (GroupPortAttack)GetValue(PortAttackProperty); 
            }
            set 
            { 
                SetValue(PortAttackProperty, value); 
            }
        }

        // Using a DependencyProperty as the backing store for PortAttack.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PortAttackProperty =
            DependencyProperty.Register("PortAttack", typeof(GroupPortAttack), typeof(PortBasedAttacks), new PropertyMetadata(default(GroupPortAttack)));


    }
}
