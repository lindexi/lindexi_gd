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
using Windows.Storage.Pickers;
using Microsoft.Win32;

namespace NairnukemlodarjurNidinawjalichaw;

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
        //var folderPicker = new FolderPicker();
        //folderPicker.PickSingleFolderAsync();

        //var openFolderDialog = new OpenFolderDialog();
        //openFolderDialog.ShowDialog(this);
        //var folderName = openFolderDialog.FolderName;
    }

    private void PickFolderButton_OnClick(object sender, RoutedEventArgs e)
    {
        var folderPicker = new FolderPicker();
        folderPicker.PickSingleFolderAsync();
    }
}