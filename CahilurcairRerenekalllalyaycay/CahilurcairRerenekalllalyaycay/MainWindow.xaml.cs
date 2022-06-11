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

using Mono.Cecil;

namespace CahilurcairRerenekalllalyaycay;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();


        var assembly = Assembly.LoadFile(@"c:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App\6.0.2\WindowsBase.dll");
        var types = assembly.ExportedTypes;

        var assemblyCache = new AssemblyCache();

       var assemblyDefinition= AssemblyDefinition.ReadAssembly(
            @"c:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App\6.0.2\WindowsBase.dll",
            new ReaderParameters
            {
                ReadingMode = ReadingMode.Immediate,
                ReadSymbols = false,
                AssemblyResolver = assemblyCache
            });

       var typeDefinition = assemblyDefinition.MainModule.GetType("System.Windows.DependencyObject");
       var dispatcherPriorityDefinition = assemblyDefinition.MainModule.GetType("System.Windows.Threading", "DispatcherPriority");
       var mainModuleExportedTypes = assemblyDefinition.MainModule.ExportedTypes;


        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        TextBox.SelectionChanged += (sender, e) => MoveCustomCaret((TextBox)sender);
        TextBox.LostFocus += (sender, e) => CaretBorder.Visibility = Visibility.Collapsed;
        TextBox.GotFocus += (sender, e) => CaretBorder.Visibility = Visibility.Visible;
    }

    private void MoveCustomCaret(TextBox textBox)
    {
        var caretLocation = textBox.GetRectFromCharacterIndex(textBox.CaretIndex).Location;

        if (!double.IsInfinity(caretLocation.X))
        {
            Canvas.SetLeft(CaretBorder, caretLocation.X);
        }

        if (!double.IsInfinity(caretLocation.Y))
        {
            Canvas.SetTop(CaretBorder, caretLocation.Y);
        }
    }
}

class AssemblyCache : DefaultAssemblyResolver
{
    public AssemblyCache()
    {
        base.AddSearchDirectory(@"c:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App\6.0.2\");
        base.AddSearchDirectory(@"c:\Program Files\dotnet\shared\Microsoft.NETCore.App\6.0.2\");

    }
}
