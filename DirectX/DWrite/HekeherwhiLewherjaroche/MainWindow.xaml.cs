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

using DWrite;


namespace HekeherwhiLewherjaroche;

// https://learn.microsoft.com/en-us/answers/questions/574926/how-do-i-access-dwritecore-from-a-c-managed-applic

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
        IDWriteFactory m_pDWriteFactory = null;
        IDWriteFactory7 m_pDWriteFactory7 = null;

        Guid CLSID_DWriteFactory = new Guid("B859EE5A-D838-4B5B-A2E8-1ADC7D93DB48");
        Guid CLSID_DWriteFactory7 = new Guid("35D0E0B3-9076-4D2E-A016-A91B568A06B4");

        IntPtr pDWriteFactoryPtr = IntPtr.Zero;
        // HRESULT hr = DWriteCoreCreateFactory(DWRITE_FACTORY_TYPE.DWRITE_FACTORY_TYPE_SHARED, ref CLSID_DWriteFactory, out pDWriteFactoryPtr);  
        HRESULT hr = DWriteCoreCreateFactory(DWRITE_FACTORY_TYPE.DWRITE_FACTORY_TYPE_SHARED, ref CLSID_DWriteFactory7, out pDWriteFactoryPtr);
        if (hr == HRESULT.S_OK)
        {

        }
    }

    // As I could not get DWriteCore.dll from Microsoft.WindowAppSDK.DWrite package when I tested,  
    // I got it from https://github.com/microsoft/WindowsAppSDK-Samples/tree/main/Samples/TextRendering  
    // then it must be copied in executable directory  
    [DllImport("DWriteCore.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern HRESULT DWriteCoreCreateFactory(DWRITE_FACTORY_TYPE factoryType, ref Guid iid, out IntPtr factory);

    [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern int GetUserDefaultLocaleName(StringBuilder lpLocaleName, int cchLocaleName);

    public const int LOCALE_NAME_MAX_LENGTH = 85;
}