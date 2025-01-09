using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.Foundation;
using Windows.UI.Input.Inking;
using Windows.UI.Input.Inking.Analysis;
using Windows.UI.Input.Inking.Core;
using Windows.Win32;
using Windows.Win32.System.Com;
using WinRT;
using System.Runtime.InteropServices;
using Point = Windows.Foundation.Point;


namespace WerlelerefaywaNenurkecearcel;

//[Guid("062584A6-F830-4BDC-A4D2-0A10AB062B1D")]
//public unsafe partial struct InkDesktopHost : INativeGuid
//{
//    static Guid* INativeGuid.NativeGuid => (Guid*) Unsafe.AsPointer(ref Unsafe.AsRef(in CLSID_InkDesktopHost));
//}

[ComImport, Guid("4ce7d875-a981-4140-a1ff-ad93258e8d59")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public unsafe interface InkDesktopHost
{
    void QueueWorkItem(IntPtr workItem); 
    void CreateInkPresenter(Guid* riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppv); 
    void CreateAndInitializeInkPresenter([MarshalAs(UnmanagedType.Interface)] object rootVisual, float width, float height, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppv);
}

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

    private unsafe void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var winUIInk = new WinUIInk();
        winUIInk.Start();
    }

    private async void InkCanvas_OnStrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e)
    {
        var inkStrokeBuilder = new InkStrokeBuilder();
        var inkStroke = inkStrokeBuilder.CreateStroke(e.Stroke.StylusPoints.Select(t => new Point(t.X, t.Y)));
        var inkAnalyzer = new InkAnalyzer();
        inkAnalyzer.AddDataForStroke(inkStroke);
        var result = await inkAnalyzer.AnalyzeAsync();
        foreach (IInkAnalysisNode inkAnalysisNode in inkAnalyzer.AnalysisRoot.FindNodes(InkAnalysisNodeKind.InkDrawing))
        {
            var inkAnalysisInkDrawing = inkAnalysisNode as InkAnalysisInkDrawing;
            var value = inkAnalysisInkDrawing?.DrawingKind;
            if (value == InkAnalysisDrawingKind.Triangle)
            {
                MessageBox.Show("xx");
            }
        }
    }

}