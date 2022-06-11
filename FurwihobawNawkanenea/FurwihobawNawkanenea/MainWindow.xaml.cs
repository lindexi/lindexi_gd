using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FurwihobawNawkanenea
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
<<<<<<< HEAD:FurwihobawNawkanenea/FurwihobawNawkanenea/MainWindow.xaml.cs
=======

            var file = System.IO.Path.GetFullPath("Lib1.dll");
            var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(file);

            AssemblyLoadContext.Default.Resolving += Default_Resolving;
>>>>>>> 7eee4ad2 (先加载文件):CherralchenawlearLairnellukemge/CherralchenawlearLairnellukemge/MainWindow.xaml.cs
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
<<<<<<< HEAD:FurwihobawNawkanenea/FurwihobawNawkanenea/MainWindow.xaml.cs
            var hwndSource1 = (HwndSource) PresentationSource.FromVisual(TextBox1); // not null
            var hwndSource2 = (HwndSource) PresentationSource.FromVisual(TextBox2); // null
            var logicalParent = LogicalTreeHelper.GetParent(TextBox2); // Button
            var visualParent = VisualTreeHelper.GetParent(TextBox2); // null

            if (hwndSource1 is null)
            {
                throw new ArgumentNullException(nameof(hwndSource1));
=======
            if (assemblyName.Name == "Lib1")
            {
                var stopwatch = Stopwatch.StartNew();
                var file = System.IO.Path.GetFullPath("Lib1.dll");
                var assembly = assemblyLoadContext.LoadFromAssemblyPath(file);
                stopwatch.Stop();
                var milliseconds = stopwatch.ElapsedMilliseconds;
                return assembly;
>>>>>>> 7eee4ad2 (先加载文件):CherralchenawlearLairnellukemge/CherralchenawlearLairnellukemge/MainWindow.xaml.cs
            }

            if (hwndSource2 is null)
            {
                throw new ArgumentNullException(nameof(hwndSource2));
            }
        }
    }
}
