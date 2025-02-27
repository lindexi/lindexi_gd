using System;
using System.Threading.Tasks;
using LightTextEditorPlus.Core.Rendering;

namespace LightTextEditorPlus.Core;

/// <summary>
/// 文本布局扩展方法
/// </summary>
public static class TextEditorCoreTextLayoutExtensions
{
    /// <summary>
    /// 在文本布局完成没有脏的时候执行逻辑。由于文本可能被业务层污染，如某些业务在 <see cref="TextEditorCore.LayoutCompleted"/> 事件里面修改文本，导致难以一次等待就拿到不脏的文本
    /// </summary>
    /// <param name="textEditor"></param>
    /// <param name="action"></param>
    /// <returns></returns>
    public static async ValueTask DoWhenLayoutCompletedNotDirtyAsync(this TextEditorCore textEditor,
        Action<RenderInfoProvider> action)
    {
        while (textEditor.IsDirty)
        {
            await textEditor.WaitLayoutCompletedAsync();
        }

        RenderInfoProvider renderInfoProvider = textEditor.GetRenderInfo();
        action(renderInfoProvider);
    }
}