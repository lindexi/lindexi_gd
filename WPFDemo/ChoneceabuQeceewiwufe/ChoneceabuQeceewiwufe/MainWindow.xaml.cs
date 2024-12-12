using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using InkPresenter = Windows.UI.Input.Inking.InkPresenter;
using Point = Windows.Foundation.Point;

namespace ChoneceabuQeceewiwufe;
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
        var thread = new Thread(() =>
        {
            global::WinRT.ComWrappersSupport.InitializeComWrappers();

            // 如果不在 csproj 加上
            // <WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>
            // <WindowsPackageType>None</WindowsPackageType>
            // 将抛出 没有注册类 (0x80040154 (REGDB_E_CLASSNOTREG))
            global::Microsoft.UI.Xaml.Application.Start(p =>
            {
                // 0x8001010e
                var coreInkPresenterHost = new CoreInkPresenterHost();
            });

        });
        thread.IsBackground = true;
        thread.Start();
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
