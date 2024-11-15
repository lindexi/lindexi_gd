using System.Runtime.InteropServices;
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

namespace WilinojearcheWheyecearhire;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Button_OnClick(object sender, RoutedEventArgs e)
    {
        var filePath = InputTextBox.Text;
        filePath = System.IO.Path.GetFullPath(filePath);

        IntPtr pidlList = ILCreateFromPathW(filePath);
        if (pidlList != IntPtr.Zero)
        {
            try
            {
                // Open parent folder and select item
                Marshal.ThrowExceptionForHR(SHOpenFolderAndSelectItems(pidlList, 0, IntPtr.Zero, 0));
            }
            finally
            {
                ILFree(pidlList);
            }
        }
    }

    //[DllImport("shell32.dll", ExactSpelling = true)]
    //private static extern void ILFree(IntPtr pidlList);

    //[DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    //private static extern IntPtr ILCreateFromPathW(string pszPath);

    //[DllImport("shell32.dll", ExactSpelling = true)]
    //private static extern int SHOpenFolderAndSelectItems(IntPtr pidlList, uint cild, IntPtr children, uint dwFlags);

    [LibraryImport("shell32.dll")]
    private static partial void ILFree(IntPtr pidlList);

    [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial IntPtr ILCreateFromPathW(string pszPath);

    [LibraryImport("shell32.dll")]
    private static partial int SHOpenFolderAndSelectItems(IntPtr pidlList, uint cild, IntPtr children, uint dwFlags);
}