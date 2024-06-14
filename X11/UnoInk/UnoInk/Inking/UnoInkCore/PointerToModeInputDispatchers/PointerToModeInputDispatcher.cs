using System.Threading.Channels;
using UnoInk.Inking.InkCore.Interactives;
using UnoInk.Inking.X11Ink;

namespace UnoInk.UnoInkCore;

/// <summary>
/// 从 <see cref="PointerInputInfo"/> 调度到模式输入层
/// </summary>
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
        while (true)
        {
            var waitToRead = await Reader.WaitToReadAsync().ConfigureAwait(false);
            if (!waitToRead)
            {
                break;
            }
            
            //await InvokeAsync(canvas =>
            //{
            //    while (true)
            //    {
            //        if (!_channel.Reader.TryRead(out var info))
            //        {
            //            break;
            //        }
            //    }
                
            //});
        }
    }
}
