// ReSharper disable once CheckNamespace

namespace LightTextEditorPlus.Platform;

/// <summary>
/// 文本库的平台提供者创建器
/// </summary>
public class TextEditorPlatformProviderBuilder : ITextEditorPlatformProviderBuilder
{
    /// <inheritdoc />
    public virtual TextEditorPlatformProvider Build(TextEditor textEditor)
    {
        return new TextEditorPlatformProvider(textEditor);
    }
}

/// <summary>
/// 文本库的平台提供者创建器
/// </summary>
public interface ITextEditorPlatformProviderBuilder
{
    /// <summary>
    /// 构建关联文本对象的文本库的平台提供者
    /// </summary>
    /// <param name="textEditor"></param>
    /// <returns></returns>
    TextEditorPlatformProvider Build(TextEditor textEditor);
}