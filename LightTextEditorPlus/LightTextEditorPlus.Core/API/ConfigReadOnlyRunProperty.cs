using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Core;

///// <summary>
///// 字符属性的配置委托
///// </summary>
//public delegate IReadOnlyRunProperty ConfigReadOnlyRunProperty(IReadOnlyRunProperty baseRunProperty);

/// <summary>
/// 字符属性的配置委托
/// </summary>
public delegate T ConfigReadOnlyRunProperty<T>(T baseRunProperty) where T : IReadOnlyRunProperty;