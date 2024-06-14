using System.Diagnostics;
using System.Runtime.Versioning;
using System.Threading.Channels;

using UnoInk.Inking.InkCore.Interactives;
using UnoInk.Inking.X11Ink;
using UnoInk.Inking.X11Platforms;

namespace UnoInk.UnoInkCore;

/// <summary>
/// 从 <see cref="PointerInputInfo"/> 调度到模式输入层
/// </summary>
[SupportedOSPlatform("Linux")]
class PointerToModeInputDispatcher
{
    public PointerToModeInputDispatcher(ChannelReader<PointerInputInfo> reader, X11InkWindow x11InkWindow)
    {
        Reader = reader;
        X11InkWindow = x11InkWindow;
    }

    private ChannelReader<PointerInputInfo> Reader { get; }
    public X11InkWindow X11InkWindow { get; }
    private ModeInputDispatcher ModeInputDispatcher => X11InkWindow.ModeInputDispatcher;

    public async Task RunAsync()
    {
        int count = 0;
        var stopwatch = new Stopwatch();
        long time = 0;

        while (true)
        {
            var waitToRead = await Reader.WaitToReadAsync()
                // 防止回到主线程
                // 这里随意的后台线程就能处理
                .ConfigureAwait(false);
            if (!waitToRead)
            {
                break;
            }

            await X11InkWindow.InvokeAsync(canvas =>
            {
                bool isFirst = true;
                PointerInputInfo lastInputInfo = default;
                while (true)
                {
                    if (!Reader.TryRead(out PointerInputInfo info))
                    {
                        break;
                    }
                    
                    if (Reader.Count > 0)
                    {
                        StaticDebugLogger.WriteLine($"卡顿率 {Reader.Count}");
                    }

                    if (info.Type == PointerInputType.Down)
                    {
                        ModeInputDispatcher.Down(info.InputArgs);
                    }
                    else if (info.Type == PointerInputType.Move)
                    {
                        stopwatch.Restart();
                        ModeInputDispatcher.Move(info.InputArgs);
                        stopwatch.Stop();
                        time += stopwatch.ElapsedTicks;
                        count++;
                        if (count >= 100)
                        {
                            StaticDebugLogger.WriteLine($"平均移动耗时 {time * 1.0 / count / Stopwatch.Frequency * 1000}");
                            
                            time = 0;
                            count = 0;
                        }
                    }
                    else if (info.Type == PointerInputType.Up)
                    {
                        ModeInputDispatcher.Up(info.InputArgs);
                    }
                    else
                    {
                        Debug.Fail("没有考虑过的逻辑");
                    }

                    if (!isFirst)
                    {

                    }

                    isFirst = false;
                    lastInputInfo = info;
                }

            }).ConfigureAwait(false);
        }
    }
}
