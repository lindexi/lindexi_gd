using System.Reflection;
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

namespace ChedemwoheGelnairkoni;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var assemblyInformationalVersionAttribute = GetType().Assembly.GetCustomAttributes<System.Reflection.AssemblyInformationalVersionAttribute>().First();
        AppVersionRun.Text = assemblyInformationalVersionAttribute.InformationalVersion;
    }
}