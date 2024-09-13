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

        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        IDWriteFactory dwriteFactory = null;
        IDWriteFactory7 dwriteFactory7 = null;

        Guid CLSID_DWriteFactory = new Guid("B859EE5A-D838-4B5B-A2E8-1ADC7D93DB48");
        Guid CLSID_DWriteFactory7 = new Guid("35D0E0B3-9076-4D2E-A016-A91B568A06B4");

        IntPtr pDWriteFactoryPtr = IntPtr.Zero;
        // HRESULT hr = DWriteCoreCreateFactory(DWRITE_FACTORY_TYPE.DWRITE_FACTORY_TYPE_SHARED, ref CLSID_DWriteFactory, out pDWriteFactoryPtr);  
        HRESULT hr = DWriteCoreCreateFactory(DWRITE_FACTORY_TYPE.DWRITE_FACTORY_TYPE_SHARED, ref CLSID_DWriteFactory7, out pDWriteFactoryPtr);
        if (hr == HRESULT.S_OK)
        {
            // m_pDWriteFactory = Marshal.GetObjectForIUnknown(pDWriteFactoryPtr) as IDWriteFactory;  
            dwriteFactory7 = Marshal.GetObjectForIUnknown(pDWriteFactoryPtr) as IDWriteFactory7;
            //IDWriteFontCollection pFontCollection;  
            //hr = m_pDWriteFactory.GetSystemFontCollection(out pFontCollection);  
            IDWriteFontCollection3 pFontCollection;
            hr = dwriteFactory7.GetSystemFontCollection7(false, DWRITE_FONT_FAMILY_MODEL.DWRITE_FONT_FAMILY_MODEL_TYPOGRAPHIC, out pFontCollection);
            if (hr == HRESULT.S_OK)
            {
                uint nFamilyCount = pFontCollection.GetFontFamilyCount();
                for (uint i = 0; i < nFamilyCount; i++)
                {
                    IDWriteFontFamily pFontFamily;
                    hr = pFontCollection.GetFontFamily(i, out pFontFamily);
                    IDWriteLocalizedStrings pFamilyNames;
                    pFontFamily.GetFamilyNames(out pFamilyNames);

                    uint nIndex = 0;
                    bool bExists = false;
                    StringBuilder sbLocaleName = new StringBuilder(LOCALE_NAME_MAX_LENGTH);
                    int nDefaultLocaleSuccess = GetUserDefaultLocaleName(sbLocaleName, LOCALE_NAME_MAX_LENGTH);
                    if (nDefaultLocaleSuccess > 0)
                    {
                        hr = pFamilyNames.FindLocaleName(sbLocaleName.ToString(), out nIndex, out bExists);
                    }
                    if (hr == HRESULT.S_OK && !bExists)
                    {
                        hr = pFamilyNames.FindLocaleName("en-us", out nIndex, out bExists);
                    }
                    if (!bExists)
                        nIndex = 0;
                    hr = pFamilyNames.GetString(nIndex, sbLocaleName, LOCALE_NAME_MAX_LENGTH);
                    string sName = sbLocaleName.ToString();

                    System.Diagnostics.Debug.WriteLine("Font : " + sName);

                    Marshal.ReleaseComObject(pFamilyNames);
                    Marshal.ReleaseComObject(pFontFamily);
                }
                Marshal.ReleaseComObject(pFontCollection);
            }
            Marshal.ReleaseComObject(dwriteFactory7);
        }
    }

    private void Button_OnClick(object sender, RoutedEventArgs e)
    {
        
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