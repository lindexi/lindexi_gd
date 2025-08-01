using System;

namespace LightTextEditorPlus.Core.TestsFramework;

/// <summary>
/// 手动触发布局更新
/// </summary>
public class ManuallyRequireLayoutDispatcher
{
    public Action? CurrentLayoutAction { get; private set; }

    public void InvokeLayoutAction()
    {
        CurrentLayoutAction?.Invoke();
    }

    internal void RequireDispatchUpdateLayout(Action updateLayoutAction)
    {
        CurrentLayoutAction = updateLayoutAction;
    }
}
