using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Document;

/// <summary>
/// 创建一个新的 <see cref="styleRunProperty"/> 对象的委托
/// </summary>
/// <param name="styleRunProperty"></param>
/// <returns></returns>
public delegate ParagraphProperty CreateParagraphPropertyDelegate(ParagraphProperty styleRunProperty);