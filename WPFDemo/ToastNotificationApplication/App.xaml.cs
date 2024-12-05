using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using dotnetCampus.Cli;

namespace ToastNotificationApplication;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        var option = CommandLine.Parse(e.Args).As<Option>();

        var applicationName = option.ApplicationName ?? "应用";

        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 15063))
        {
            global::WinRT.ComWrappersSupport.InitializeComWrappers();

            // 以下 XML 的构建，请看
            // https://learn.microsoft.com/en-us/windows/apps/design/shell/tiles-and-notifications/adaptive-interactive-toasts?tabs=xml
            var xmlDocument = new XmlDocument();
            var toast = GetToast();
            if (toast is null)
            {
                Shutdown(-1);
            }

            xmlDocument.LoadXml(xml: toast);

            var toastNotification = new ToastNotification(xmlDocument);
            var toastNotificationManagerForUser = ToastNotificationManager.GetDefault();
            var toastNotifier = toastNotificationManagerForUser.CreateToastNotifier(applicationId: applicationName);
            toastNotifier.Show(toastNotification);
            Shutdown();
        }

        string? GetToast()
        {
            if (option.XmlFilePath is not null && File.Exists(option.XmlFilePath))
            {
                return File.ReadAllText(option.XmlFilePath);
            }

            if (!string.IsNullOrEmpty(option.Text))
            {
                // lang=xml
                return $"""
                        <toast>
                            <visual>
                                <binding template='ToastText01'>
                                    <text id="1">{option.Text}</text>
                                </binding>
                            </visual>
                        </toast>
                        """;
            }

            return null;
        }
    }
}

class Option
{
    [Option("ApplicationName")]
    public string? ApplicationName { get; set; }

    [Option("XmlFilePath")]
    public string? XmlFilePath { get; set; }

    [Option("Text")]
    public string? Text { get; set; }
}

