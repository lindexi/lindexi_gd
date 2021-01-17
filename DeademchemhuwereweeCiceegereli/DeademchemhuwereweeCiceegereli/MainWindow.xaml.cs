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
using System.Windows.Input.Manipulations;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DeademchemhuwereweeCiceegereli
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            var manipulationProcessor2D = new ManipulationProcessor2D(Manipulations2D.All);
            manipulationProcessor2D.SetParameters(new ManipulationPivot2D()
            {

            });
            manipulationProcessor2D.ProcessManipulators(0, new Manipulator2D[]
            {
                new Manipulator2D(1,0,0),
            });
            InitializeComponent();
        }
    }
}
