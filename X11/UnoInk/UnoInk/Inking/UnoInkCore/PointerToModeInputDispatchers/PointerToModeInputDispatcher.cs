using System.Diagnostics;
using System.Runtime.Versioning;
using System.Threading.Channels;

using UnoInk.Inking.InkCore;
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
                while (true)
                {
                    if (!Reader.TryRead(out PointerInputInfo info))
                    {
                        break;
                    }

                    if (info.Type == PointerInputType.Down)
                    {
                        ModeInputDispatcher.Down(info.InputArgs);
                    }
                    else if (info.Type == PointerInputType.Move)
                    {
                        // 合并多个输入提升性能
                        stopwatch.Restart();
                        var inputArgs = MergeMove(info.InputArgs);
                        ModeInputDispatcher.Move(in inputArgs);
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
                }

            }).ConfigureAwait(false);
        }
    }

    private ModeInputArgs MergeMove(ModeInputArgs currentInputArgs)
    {
        List<StylusPoint>? list = null;

        while (true)
        {
            if (!Reader.TryPeek(out var info))
            {
                break;
            }

            if (info.Type != PointerInputType.Move)
            {
                break;
            }

            var inputArgs = info.InputArgs;

            if (inputArgs.Id != currentInputArgs.Id)
            {
                break;
            }

            if (list == null)
            {
                list = [currentInputArgs.StylusPoint, inputArgs.StylusPoint];
            }
            else
            {
                list.Add(inputArgs.StylusPoint);
            }
            
            // 如果是能够合并的，那就读走
            Reader.TryRead(out _);
        }

        return currentInputArgs with
        {
            StylusPointList = list
        };
    }
}
