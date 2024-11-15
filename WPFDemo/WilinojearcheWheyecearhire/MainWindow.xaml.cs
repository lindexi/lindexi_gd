using System.IO;
using System.Reflection.PortableExecutable;
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
        // 必定返回失败，因为 WPF 已经调用过了
        var result = CoInitialize(0, 0);

        var folderPath = InputTextBox.Text;

        folderPath = System.IO.Path.GetFullPath(folderPath);

        IntPtr pidlList = ILCreateFromPathW(folderPath);


        if (pidlList != IntPtr.Zero)
        {
            var fileList = Directory.GetFiles(folderPath);

            var selectedFileList = new IntPtr[fileList.Length];

            for (var i = 0; i < fileList.Length; i++)
            {
                var file = fileList[i];
                selectedFileList[i] = ILCreateFromPathW(file);
            }

            try
            {
                // Open parent folder and select item
                Marshal.ThrowExceptionForHR(SHOpenFolderAndSelectItems(pidlList, (uint) fileList.Length, selectedFileList, 0));
            }
            finally
            {
                ILFree(pidlList);

                foreach (var nint in selectedFileList)
                {
                    ILFree(nint);
                }
            }
        }
    }

    //[DllImport("shell32.dll", ExactSpelling = true)]
    //private static extern void ILFree(IntPtr pidlList);

    //[DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    //private static extern IntPtr ILCreateFromPathW(string pszPath);

    //[DllImport("shell32.dll", ExactSpelling = true)]
    //private static extern int SHOpenFolderAndSelectItems(IntPtr pidlList, uint cild, IntPtr children, uint dwFlags);

    [LibraryImport("Ole32.dll")]
    private static partial int CoInitialize(IntPtr pvReserved, uint dwCoInit);

    [LibraryImport("shell32.dll")]
    private static partial void ILFree(IntPtr pidlList);

    [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial IntPtr ILCreateFromPathW(string pszPath);

    [LibraryImport("shell32.dll")]
    private static partial int SHOpenFolderAndSelectItems(IntPtr pidlList, uint cild, [MarshalAs(UnmanagedType.LPArray)] IntPtr[] children, uint dwFlags);
}