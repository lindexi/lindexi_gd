// ReSharper disable once CheckNamespace
namespace LightTextEditorPlus;

/// <summary>
/// 文本库的平台提供者创建器
/// </summary>
public class TextEditorPlatformProviderBuilder
{
    /// <summary>
    /// 构建关联文本对象的文本库的平台提供者
    /// </summary>
    /// <param name="textEditor"></param>
    /// <returns></returns>
    public virtual TextEditorPlatformProvider Build(TextEditor textEditor)
    {
        return new TextEditorPlatformProvider(textEditor);
    }
}