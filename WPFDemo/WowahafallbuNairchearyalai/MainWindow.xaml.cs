using System.Diagnostics;
using Microsoft.Office.Interop.Word;

using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
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
using System.Windows.Threading;
using Application = Microsoft.Office.Interop.Word.Application;
using Image = System.Windows.Controls.Image;
using Page = Microsoft.Office.Interop.Word.Page;
using Path = System.IO.Path;
using Task = System.Threading.Tasks.Task;
using Window = System.Windows.Window;

namespace WowahafallbuNairchearyalai;

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
        var lastCaption = string.Empty;
        Task.Run(async () =>
        {
            while (true)
            {
                // 附着到当前运行的 Word 进程（而不是新建）
                ApplicationClass? app = null;
                Application? application = null;
                try
                {
                    var currentApplication = (Application) Marshal2.GetActiveObject("Word.Application");
                    app = currentApplication as ApplicationClass;
                    application = currentApplication;
                }
                catch (COMException exception)
                {
                    WriteLogMessage($"获取 Word 失败 {exception.Message}");
                    // 未运行 Word，直接返回或根据需要提示
                    return;
                }

                _application = application;

                application.WindowActivate += App_ApplicationEvents2_Event_WindowActivate;

                _app = app;

                try
                {
                    var activeWordDocumentInfoList = new List<WordDocumentInfo>();
                    foreach (Microsoft.Office.Interop.Word.Window appWindow in application.Windows)
                    {
                        if (appWindow.Active)
                        {
                            WriteLogMessage($"当前激活窗口《{appWindow.Caption}》 Hwnd={appWindow.Hwnd:X}");

                            WordDocumentInfo? wordDocumentInfo =
                                WordDocumentInfoList.FirstOrDefault(t => t.Caption == appWindow.Caption);

                            if (wordDocumentInfo is null)
                            {
                                wordDocumentInfo = GetWordDocumentInfo(appWindow);
                                WordDocumentInfoList.Add(wordDocumentInfo);
                            }

                            activeWordDocumentInfoList.Add(wordDocumentInfo);
                        }
                    }

                    if (activeWordDocumentInfoList.Count == 1)
                    {
                        await ShowAsync(activeWordDocumentInfoList[0]);
                    }
                    else if (activeWordDocumentInfoList.Count > 0)
                    {
                        var message = $"当前检测到 {activeWordDocumentInfoList.Count} 个窗口。";
                        var foregroundWindow = GetForegroundWindow();
                        message += $"前台窗口={foregroundWindow:X}。";

                        var wordDocumentInfo = activeWordDocumentInfoList.FirstOrDefault(t => t.Hwnd == foregroundWindow);
                        if (wordDocumentInfo is null)
                        {
                            message += $"无任何文档满足前台窗口。文档={string.Join(';', activeWordDocumentInfoList.Select(t => t.Hwnd.ToString("X")))}";
                        }
                        else
                        {
                            message += $"当前激活窗口《{wordDocumentInfo.Caption}》";
                            await ShowAsync(wordDocumentInfo);
                        }

                        WriteLogMessage(message);
                    }

                    async Task ShowAsync(WordDocumentInfo wordDocumentInfo)
                    {
                        if (wordDocumentInfo.Caption == lastCaption)
                        {
                            return;
                        }

                        lastCaption = wordDocumentInfo.Caption;

                        await ShowWordDocumentInfoAsync(wordDocumentInfo);
                    }
                }
                catch (Exception exception)
                {
                    Debug.WriteLine(exception);
                }

                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        });
    }

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    //[DllImport("user32.dll")]
    //public static extern IntPtr GetActiveWindow(); // 当前线程


    private async Task ShowWordDocumentInfoAsync(WordDocumentInfo wordDocumentInfo)
    {
        var list = new List<BitmapImage>();

        foreach (var imageFile in wordDocumentInfo.PageImageList)
        {
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            //bitmapImage.StreamSource = ms;
            bitmapImage.UriSource = new Uri(imageFile.FullName);
            bitmapImage.EndInit();
            bitmapImage.Freeze();

            list.Add(bitmapImage);
        }

        await Dispatcher.InvokeAsync(() =>
        {
            ListView.Items.Clear();

            var width = ListView.ActualWidth;

            foreach (var bitmapImage in list)
            {
                var height = bitmapImage.PixelHeight * (width / bitmapImage.PixelWidth);

                ListView.Items.Add(new Image()
                {
                    Source = bitmapImage,
                    Width = width,
                    Height = height,
                    Stretch = Stretch.Fill
                });
            }
        });
    }

    private WordDocumentInfo GetWordDocumentInfo(Microsoft.Office.Interop.Word.Window appWindow)
    {
        var wordPageImageFileList = GetWordPageImageFileList(appWindow.Panes);
        var wordDocumentInfo = new WordDocumentInfo()
        {
            Caption = appWindow.Caption,
            Hwnd = appWindow.Hwnd,
            PageImageList = wordPageImageFileList
        };
        return wordDocumentInfo;
    }

    private ApplicationClass? _app;
    private Application? _application;

    private List<WordDocumentInfo> WordDocumentInfoList { get; } = [];

    private void WriteLogMessage(string log)
    {
        Dispatcher.InvokeAsync(() =>
        {
            LogTextBlock.Text = log;
        }, DispatcherPriority.Send);
    }

    private void App_ApplicationEvents2_Event_WindowActivate(Document doc, Microsoft.Office.Interop.Word.Window wn)
    {
        //Panes? documentWindowPanes = wn.Panes;
        //ShowWord(documentWindowPanes);
    }

    private IReadOnlyList<FileInfo> GetWordPageImageFileList(Panes? documentWindowPanes)
    {
        if (documentWindowPanes is null)
        {
            return [];
        }

        var workFolder = Path.Join(AppContext.BaseDirectory, $"Image_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}");
        Directory.CreateDirectory(workFolder);

        var list = new List<FileInfo>();

        var documentWindowPanesCount = documentWindowPanes.Count;
        for (var index = 0; index < documentWindowPanesCount; index++)
        {
            Pane documentWindowPane = documentWindowPanes[index + 1];
            var pagesCount = documentWindowPane.Pages.Count;
            for (int i = 0; i < pagesCount; i++)
            {
                Page? page = documentWindowPane.Pages[i + 1];

                var bits = page.EnhMetaFileBits;

                var ms = new MemoryStream((byte[]) (bits));

                var imageFile = Path.Join(workFolder, $"{i}.png");
                var image = System.Drawing.Image.FromStream(ms);
                image.Save(imageFile, ImageFormat.Png);

                //var bitmapImage = new BitmapImage();
                //bitmapImage.BeginInit();
                ////bitmapImage.StreamSource = ms;
                //bitmapImage.UriSource = new Uri(imageFile);
                //bitmapImage.EndInit();
                //bitmapImage.Freeze();

                //list.Add(bitmapImage);
                list.Add(new FileInfo(imageFile));
            }
        }

        //Dispatcher.InvokeAsync(() =>
        //{
        //    ListView.Items.Clear();

        //    var width = ListView.ActualWidth;

        //    foreach (var bitmapImage in list)
        //    {
        //        var height = bitmapImage.PixelHeight * (width / bitmapImage.PixelWidth);

        //        ListView.Items.Add(new Image()
        //        {
        //            Source = bitmapImage,
        //            Width = width,
        //            Height = height,
        //            Stretch = Stretch.Fill
        //        });
        //    }
        //});
        return list;
    }
}

public record WordDocumentInfo
{
    public required string Caption { get; init; }
    public required int Hwnd { get; init; }
    public required IReadOnlyList<FileInfo> PageImageList { get; init; }
}

// Source - https://stackoverflow.com/a/65496277
// Posted by Kriss_Kross
// Retrieved 2025-12-09, License - CC BY-SA 4.0
public static class Marshal2
{
    internal const String OLEAUT32 = "oleaut32.dll";
    internal const String OLE32 = "ole32.dll";

    [System.Security.SecurityCritical]  // auto-generated_required
    public static Object GetActiveObject(String progID)
    {
        Object obj = null;
        Guid clsid;

        // Call CLSIDFromProgIDEx first then fall back on CLSIDFromProgID if
        // CLSIDFromProgIDEx doesn't exist.
        try
        {
            CLSIDFromProgIDEx(progID, out clsid);
        }
        //            catch
        catch (Exception)
        {
            CLSIDFromProgID(progID, out clsid);
        }

        GetActiveObject(ref clsid, IntPtr.Zero, out obj);
        return obj;
    }

    //[DllImport(Microsoft.Win32.Win32Native.OLE32, PreserveSig = false)]
    [DllImport(OLE32, PreserveSig = false)]
    [ResourceExposure(ResourceScope.None)]
    [SuppressUnmanagedCodeSecurity]
    [System.Security.SecurityCritical]  // auto-generated
    private static extern void CLSIDFromProgIDEx([MarshalAs(UnmanagedType.LPWStr)] String progId, out Guid clsid);

    //[DllImport(Microsoft.Win32.Win32Native.OLE32, PreserveSig = false)]
    [DllImport(OLE32, PreserveSig = false)]
    [ResourceExposure(ResourceScope.None)]
    [SuppressUnmanagedCodeSecurity]
    [System.Security.SecurityCritical]  // auto-generated
    private static extern void CLSIDFromProgID([MarshalAs(UnmanagedType.LPWStr)] String progId, out Guid clsid);

    //[DllImport(Microsoft.Win32.Win32Native.OLEAUT32, PreserveSig = false)]
    [DllImport(OLEAUT32, PreserveSig = false)]
    [ResourceExposure(ResourceScope.None)]
    [SuppressUnmanagedCodeSecurity]
    [System.Security.SecurityCritical]  // auto-generated
    private static extern void GetActiveObject(ref Guid rclsid, IntPtr reserved, [MarshalAs(UnmanagedType.Interface)] out Object ppunk);

}
