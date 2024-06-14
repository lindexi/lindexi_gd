using System.Threading.Channels;
using UnoInk.Inking.InkCore.Interactives;

namespace UnoInk.UnoInkCore;

/// <summary>
/// 从 <see cref="PointerInputInfo"/> 调度到模式输入层
/// </summary>
class PointerToModeInputDispatcher
{
    public PointerToModeInputDispatcher(ChannelReader<PointerInputInfo> reader, ModeInputDispatcher modeInputDispatcher)
    {
        Reader = reader;
        ModeInputDispatcher = modeInputDispatcher;
    }
    
    private ChannelReader<PointerInputInfo> Reader { get; }
    private ModeInputDispatcher ModeInputDispatcher { get; }
}
