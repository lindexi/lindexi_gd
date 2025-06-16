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
using System.Windows.Threading;

using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;

namespace NiqufarjeKajurnelkeba;

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

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var thread = new Thread(() =>
        {
            bool requestDispatch = false;
            while (true)
            {
                HRESULT hResult = Windows.Win32.PInvoke.DwmFlush();
                if (hResult.Failed)
                {
                    break;
                }

                if (requestDispatch is false)
                {
                    requestDispatch = true;
                    Dispatcher.InvokeAsync(() =>
                    {
                        Update();

                        requestDispatch = false;
                    }, DispatcherPriority.Render);
                }
            }
        });
        thread.IsBackground = true;
        thread.Start();
    }

    private unsafe void Update()
    {
        DWM_TIMING_INFO dwmTimingInfo = new DWM_TIMING_INFO()
        {
            cbSize = (uint) Marshal.SizeOf<DWM_TIMING_INFO>()
        };
        HRESULT hResult = Windows.Win32.PInvoke.DwmGetCompositionTimingInfo(HWND.Null, &dwmTimingInfo);
        hResult.ThrowOnFailure();

        /*
          rateRefresh

          监视器刷新率。

          qpcRefreshPeriod

          监视器刷新期。

          rateCompose

          组合速率。

          qpcVBlank

          垂直空白前的查询性能计数器值。

          cRefresh

          DWM 刷新计数器。

          cDXRefresh

          DirectX 刷新计数器。

          qpcCompose

          帧组合的查询性能计数器值。

          cFrame

          在 qpcCompose 中构成的帧编号。

          cDXPresent

          用于标识呈现帧的 DirectX 当前编号。

          cRefreshFrame

          在 qpcCompose 中撰写的帧的刷新计数。

          cFrameSubmitted

          上次提交的 DWM 帧编号。

          cDXPresentSubmitted

          上次提交的 DirectX 当前编号。

          cFrameConfirmed

          上次确认为显示的 DWM 帧编号。

          cDXPresentConfirmed

          上次确认的 DirectX 当前编号。

          cRefreshConfirmed

          GPU 确认为已完成的最后一帧的目标刷新计数。

          cDXRefreshConfirmed

          确认帧显示的 DirectX 刷新计数。

          cFramesLate

          DWM 延迟显示的帧数。

          cFramesOutstanding

          已发出但尚未确认为已完成的合成帧数。

          cFrameDisplayed

          显示的最后一帧。

          qpcFrameDisplayed

          显示帧时合成的 QPC 时间通过。

          cRefreshFrameDisplayed

          帧应变为可见时的垂直刷新计数。

          cFrameComplete

          最后一个帧标记为已完成的 ID。

          qpcFrameComplete

          最后一帧标记为已完成的 QPC 时间。

          cFramePending

          最后一个帧标记为挂起的 ID。

          qpcFramePending

          最后一帧标记为挂起的 QPC 时间。

          cFramesDisplayed

          显示的唯一帧数。 此值仅在第二次调用 DwmGetCompositionTimingInfo 函数后有效。

          cFramesComplete

          已接收的新已完成帧数。

          cFramesPending

          提交到 DirectX 但尚未完成的新帧数。

          cFramesAvailable

          可用但未显示、已使用或已删除的帧数。 此值仅在第二次调用 DwmGetCompositionTimingInfo 后有效。

          cFramesDropped

          由于合成发生太晚而从未显示的渲染帧数。 此值仅在第二次调用 DwmGetCompositionTimingInfo 后有效。

          cFramesMissed

          本应使用新帧但不可用时，旧框架被组合的次数。

          cRefreshNextDisplayed

          计划显示下一帧的帧计数。

          cRefreshNextPresented

          计划显示下一个 DirectX 的帧计数。

          cRefreshesDisplayed

          自上次调用 DwmSetPresentParameters 函数以来为应用程序显示的刷新总数。

          cRefreshesPresented

          自上次调用 DwmSetPresentParameters 以来，应用程序提供的刷新总数。

          cRefreshStarted

          开始显示此窗口内容时的刷新数。

          cPixelsReceived

          DirectX 重定向到 DWM 的像素总数。

          cPixelsDrawn

          绘制的像素数。

          cBuffersEmpty

          翻转链中的空缓冲区数。
        */
        var text =
           $"""
             监视器刷新率 rateRefresh: {ToText(dwmTimingInfo.rateRefresh)}
             监视器刷新期 qpcRefreshPeriod: {dwmTimingInfo.qpcRefreshPeriod} ticks
             组合速率 rateCompose: {ToText(dwmTimingInfo.rateCompose)} Hz
             垂直空白前的查询性能计数器值 qpcVBlank: {dwmTimingInfo.qpcVBlank} ticks
             DirectX 刷新计数器 cDXRefresh: {dwmTimingInfo.cDXRefresh}
             帧组合的查询性能计数器值 qpcCompose: {dwmTimingInfo.qpcCompose} ticks
             在 qpcCompose 中构成的帧编号 cFrame: {dwmTimingInfo.cFrame}
             用于标识呈现帧的 DirectX 当前编号 cDXPresent: {dwmTimingInfo.cDXPresent}
             在 qpcCompose 中撰写的帧的刷新计数 cRefreshFrame: {dwmTimingInfo.cRefreshFrame}
             上次提交的 DWM 帧编号 cFrameSubmitted: {dwmTimingInfo.cFrameSubmitted}
             上次提交的 DirectX 当前编号 cDXPresentSubmitted: {dwmTimingInfo.cDXPresentSubmitted}
             上次确认为显示的 DWM 帧编号 cFrameConfirmed: {dwmTimingInfo.cFrameConfirmed}
             上次确认的 DirectX 当前编号 cDXPresentConfirmed: {dwmTimingInfo.cDXPresentConfirmed}
             GPU 确认为已完成的最后一帧的目标刷新计数 cRefreshConfirmed: {dwmTimingInfo.cRefreshConfirmed}
             确认帧显示的 DirectX 刷新计数 cDXRefreshConfirmed: {dwmTimingInfo.cDXRefreshConfirmed}
             DWM 延迟显示的帧数 cFramesLate: {dwmTimingInfo.cFramesLate}
             已发出但尚未确认为已完成的合成帧数 cFramesOutstanding: {dwmTimingInfo.cFramesOutstanding}
             显示的最后一帧 cFrameDisplayed: {dwmTimingInfo.cFrameDisplayed}
             显示帧时合成的 QPC 时间通过 qpcFrameDisplayed: {dwmTimingInfo.qpcFrameDisplayed} ticks
             帧应变为可见时的垂直刷新计数 cRefreshFrameDisplayed: {dwmTimingInfo.cRefreshFrameDisplayed}
             最后一个帧标记为已完成的 ID cFrameComplete: {dwmTimingInfo.cFrameComplete}
             最后一帧标记为已完成的 QPC 时间 qpcFrameComplete: {dwmTimingInfo.qpcFrameComplete} ticks
             最后一个帧标记为挂起的 ID cFramePending: {dwmTimingInfo.cFramePending}
             最后一帧标记为挂起的 QPC 时间 qpcFramePending: {dwmTimingInfo.qpcFramePending} ticks
             已接收的新已完成帧数 cFramesComplete: {dwmTimingInfo.cFramesComplete}
             提交到 DirectX 但尚未完成的新帧数 cFramesPending: {dwmTimingInfo.cFramesPending}
             可用但未显示、已使用或已删除的帧数 cFramesAvailable: {dwmTimingInfo.cFramesAvailable}
             由于合成发生太晚而从未显示的渲染帧数 cFramesDropped: {dwmTimingInfo.cFramesDropped}
             本应使用新帧但不可用时，旧框架被组合的次数 cFramesMissed: {dwmTimingInfo.cFramesMissed}
             计划显示下一帧的帧计数 cRefreshNextDisplayed: {dwmTimingInfo.cRefreshNextDisplayed}
             计划显示下一个 DirectX 的帧计数 cRefreshNextPresented: {dwmTimingInfo.cRefreshNextPresented}
             应用程序显示的刷新总数 cRefreshesDisplayed: {dwmTimingInfo.cRefreshesDisplayed}
             应用程序提供的刷新总数 cRefreshesPresented: {dwmTimingInfo.cRefreshesPresented}
             开始显示此窗口内容时的刷新数 cRefreshStarted: {dwmTimingInfo.cRefreshStarted}
             DirectX 重定向到 DWM 的像素总数 cPixelsReceived: {dwmTimingInfo.cPixelsReceived}
             绘制的像素数 cPixelsDrawn: {dwmTimingInfo.cPixelsDrawn}
             翻转链中的空缓冲区数 cBuffersEmpty: {dwmTimingInfo.cBuffersEmpty}
             """;

        TextBlock.Text = text;

        static string ToText(UNSIGNED_RATIO ratio)
        {
            return
                $"{ratio.uiDenominator * 1.0 / ratio.uiNumerator} {ratio.uiDenominator}/{ratio.uiNumerator}";
        }
    }
}