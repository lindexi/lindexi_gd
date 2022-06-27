using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

namespace LeekilerelalljarFayjululeayem
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // WindowsBase
            var assembly = typeof(DependencyObject).Assembly;

            // Switch.MS.Internal.EnableWeakEventMemoryImprovements 默认没有开启
            var baseAppContextSwitchesType = assembly.GetType("MS.Internal.BaseAppContextSwitches");

            var enableWeakEventMemoryImprovementsProperty = baseAppContextSwitchesType.GetProperty("EnableWeakEventMemoryImprovements");

            var value = enableWeakEventMemoryImprovementsProperty.GetMethod.Invoke(null, null);
        }
    }
}
