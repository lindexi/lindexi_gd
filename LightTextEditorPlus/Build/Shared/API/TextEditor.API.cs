using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Carets;

namespace LightTextEditorPlus;

// 此文件将被多个 UI 框架共享，用于约束多个 UI 框架之间使用类似或相同的 API 定义

#if !USE_SKIA

partial class TextEditor : ITextEditor
{

}

#endif

/// <summary>
/// 这个接口用于约束文本编辑器，因此是 internal 类型的，不需要被外部引用
/// </summary>
internal interface ITextEditor
{
    /// <summary>
    /// 文本核心
    /// </summary>
    TextEditorCore TextEditorCore { get; }

#if USE_WPF || USE_SKIA || USE_AVALONIA
    CaretOffset CurrentCaretOffset { get; }
    Selection CurrentSelection { get; }
#endif
}