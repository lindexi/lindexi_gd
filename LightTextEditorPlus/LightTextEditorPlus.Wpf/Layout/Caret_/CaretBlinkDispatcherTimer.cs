using System;
using System.Windows.Threading;

namespace LightTextEditorPlus.Layout;

/// <summary>
/// 用来控制光标的 <see cref="DispatcherTimer"/> 类型，同时解决业务端忘记调用 Stop 关闭光标，从而被 <see cref="DispatcherTimer"/> 内存泄露
/// </summary>
class CaretBlinkDispatcherTimer : DispatcherTimer
{
    public CaretBlinkDispatcherTimer(ICaretManager caretManager)
    {
        _caretManagerWeakReference = new WeakReference<ICaretManager>(caretManager);

        Tick += CaretBlinkDispatcherTimer_Tick;
    }

    private void CaretBlinkDispatcherTimer_Tick(object? sender, EventArgs e)
    {
        if (_caretManagerWeakReference.TryGetTarget(out var caretManager))
        {
            caretManager.OnTick();
        }
        else
        {
            // 被回收，忘记关闭 Timer 那就自己关闭了
            Stop();
        }
    }

    /// <summary>
    /// 用来解决被 <see cref="DispatcherTimer"/> 类型引用 CaretManager 类，从而内存泄露
    /// </summary>
    private readonly WeakReference<ICaretManager> _caretManagerWeakReference;
}