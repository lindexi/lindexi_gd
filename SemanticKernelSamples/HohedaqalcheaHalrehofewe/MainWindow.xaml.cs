using OpenAI;

using System.ClientModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HohedaqalcheaHalrehofewe;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Loaded+=OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var keyFile = @"C:\lindexi\Work\Doubao.txt";
        var key = File.ReadAllText(keyFile);

        var openAiClient = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions()
        {
            Endpoint = new Uri("https://ark.cn-beijing.volces.com/api/v3")
        });
    }
}