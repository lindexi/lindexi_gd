using System.Diagnostics;
using dotnetCampus.Configurations;
using dotnetCampus.Configurations.Core;

using System.IO;
using System.Runtime.InteropServices;
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

namespace NaihunojojeaKeheakabearweabe;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var coinFile = @"C:\lindexi\Nas.coin";
        if (File.Exists(coinFile))
        {
            var fileConfigurationRepo = ConfigurationFactory.FromFile(coinFile,RepoSyncingBehavior.Static);
            var appConfigurator = fileConfigurationRepo.CreateAppConfigurator();
            var nasConfiguration = appConfigurator.Of<NasConfiguration>();
            UrlTextBox.Text = nasConfiguration.Url;
            UserNameTextBox.Text = nasConfiguration.UserName;
            PasswordTextBox.Text = nasConfiguration.Password;
        }
    }

    class NasConfiguration : Configuration
    {
        public NasConfiguration() : base("")
        {
        }

        public string Url => GetString();
        public string UserName => GetString();
        public string Password => GetString();
    }

    private void ConnectButton_OnClick(object sender, RoutedEventArgs e)
    {
        var success = NetworkShare.ConnectToShare(UrlTextBox.Text, UserNameTextBox.Text, PasswordTextBox.Text);
        if (success == 0)
        {
            foreach (var fileSystemEntry in Directory.EnumerateFileSystemEntries(UrlTextBox.Text))
            {
                Debug.WriteLine(fileSystemEntry);
            }

            NetworkShare.DisconnectFromShare(UrlTextBox.Text, force: true);
        }
    }
}

public static class NetworkShare
{
    public static int ConnectToShare(string uri, string username, string password)
    {
        //Create netresource and point it at the share
        NETRESOURCE netResource = new NETRESOURCE();
        netResource.dwType = RESOURCETYPE_DISK;
        netResource.lpRemoteName = uri;

        int result = WNetUseConnection(IntPtr.Zero, netResource, password, username, 0, null, null, null);
        return result;
    }

    public static int DisconnectFromShare(string uri, bool force)
    {
        int result = WNetCancelConnection(uri, force);
        return result;
    }

    const int RESOURCETYPE_DISK = 0x00000001;
    const int CONNECT_UPDATE_PROFILE = 0x00000001;

    [StructLayout(LayoutKind.Sequential)]
    private struct NETRESOURCE()
    {
        public int dwScope = 0;
        public int dwType = 0;
        public int dwDisplayType = 0;
        public int dwUsage = 0;
        public string lpLocalName = "";
        public string lpRemoteName = "";
        public string lpComment = "";
        public string lpProvider = "";
    }

    [DllImport("Mpr.dll")]
    private static extern int WNetUseConnection
    (
        IntPtr hwndOwner,
        NETRESOURCE lpNetResource,
        string lpPassword,
        string lpUserID,
        int dwFlags,
        string? lpAccessName,
        string? lpBufferSize,
        string? lpResult
    );

    [DllImport("Mpr.dll")]
    private static extern int WNetCancelConnection
    (
        string lpName,
        bool fForce
    );
}