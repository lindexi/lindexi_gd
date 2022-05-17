using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Loader;
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

namespace CherralchenawlearLairnellukemge
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

            var file = System.IO.Path.GetFullPath("Lib1.dll");
            var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(file);

            AssemblyLoadContext.Default.Resolving += Default_Resolving;
            
        }

        private System.Reflection.Assembly? Default_Resolving(AssemblyLoadContext assemblyLoadContext, System.Reflection.AssemblyName assemblyName)
        {
            if (assemblyName.Name == "Lib1")
            {
                var stopwatch = Stopwatch.StartNew();
                var file = System.IO.Path.GetFullPath("Lib1.dll");
                var assembly = assemblyLoadContext.LoadFromAssemblyPath(file);
                stopwatch.Stop();
                var milliseconds = stopwatch.ElapsedMilliseconds;
                return assembly;
            }

            return null;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            CallFoo();
        }

        private void CallFoo()
        {
            var foo = new Foo();
            Console.WriteLine(foo);
        }
    }
}
