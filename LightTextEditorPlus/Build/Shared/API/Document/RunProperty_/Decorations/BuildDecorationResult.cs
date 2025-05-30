#if DirectTextEditorDefinition

namespace LightTextEditorPlus.Document.Decorations;

/// <summary>
/// 构建装饰的结果
/// </summary>
/// <param name="TakeCharCount">装饰层用到了多少个字符参与构建。接下来下一次调用渲染就会跳过这些字符。比如下划线可以整一片一起，那就可以快速跳过一片相同属性的字符了</param>
public readonly record struct BuildDecorationResult(int TakeCharCount)
{
#if USE_WPF
    /// <summary>
    /// 构建结果内容
    /// </summary>
    public System.Windows.Media.Drawing? Drawing { get; init; }
#elif USE_SKIA
#endif
}
#endif