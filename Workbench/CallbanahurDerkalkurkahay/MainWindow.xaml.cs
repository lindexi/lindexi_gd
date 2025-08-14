using CheejairkafeCayfelnoyikilur;

using dotnetCampus.Configurations;
using dotnetCampus.Configurations.Core;

using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Security.Cryptography.Pkcs;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace CallbanahurDerkalkurkahay;

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
        var dllFile = Path.Join(AppContext.BaseDirectory, "CairkerkugelLerehenalcaceenel.dll");
        AssemblyLoadContext.Default.Resolving += (context, name) =>
        {
            var dllFile = Path.Join(AppContext.BaseDirectory, $"{name.Name}.dll");
            if (File.Exists(dllFile))
            {
                return context.LoadFromAssemblyPath(dllFile);
            }

            return null;
        };
        Assembly assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(dllFile);
        assembly.EntryPoint?.Invoke(null, [Array.Empty<string>()]);
    }

    private void UpdateButton_OnClick(object sender, RoutedEventArgs e)
    {
        TestService.Greet = GreetTextBox.Text;
    }
}