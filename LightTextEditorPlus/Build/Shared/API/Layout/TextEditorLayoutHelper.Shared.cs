#if !USE_SKIA || USE_AllInOne

using LightTextEditorPlus.Core.Rendering;

#if USE_WPF || USE_AVALONIA // 由于当前只有在 WPF 和 Avalonia 中才有控件层的实现。因此限制在这两个框架中使用

namespace LightTextEditorPlus.Layout;

/// <summary>
/// 文本布局帮助类
/// </summary>
public static class TextEditorLayoutHelper
{
    /// <summary>
    /// 立即地获取渲染信息
    /// </summary>
    public static RenderInfoProvider GetRenderInfoImmediately(TextEditor textEditor)
    {
        // 这个帮助方法的存在只是为了隐藏起来 GetRenderInfoImmediately 方法，不能让业务层方便地调用到

        return textEditor.GetRenderInfoImmediately();
    }
}

#endif

#endif