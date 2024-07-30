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

using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace HeregemdibeHeaqereweganilai;

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
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 15063))
        {
            global::WinRT.ComWrappersSupport.InitializeComWrappers();

            // https://learn.microsoft.com/en-us/windows/apps/design/shell/tiles-and-notifications/adaptive-interactive-toasts?tabs=xml
            var xmlDocument = new XmlDocument();
            // lang=xml
            xmlDocument.LoadXml(xml: """
                                     <toast>
                                         <visual>
                                             <binding template='ToastText01'>
                                                 <text id="1">显示文本内容</text>
                                             </binding>
                                         </visual>
                                     </toast>
                                     """);

            var toastNotification = new ToastNotification(xmlDocument);
            var toastNotificationManagerForUser = ToastNotificationManager.GetDefault();
            var toastNotifier = toastNotificationManagerForUser.CreateToastNotifier(applicationId: "Foo");
            toastNotifier.Show(toastNotification);
        }
    }
}