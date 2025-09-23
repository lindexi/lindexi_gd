namespace LightTextEditorPlus.Document;

/// <summary>
/// 配置如何创建字符属性的委托
/// </summary>
/// <param name="runProperty"></param>
/// <returns></returns>
public delegate SkiaTextRunProperty ConfigRunProperty(SkiaTextRunProperty runProperty);