namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 字符信息
/// </summary>
/// <param name="CharObject"></param>
/// <param name="RunProperty"></param>
public readonly record struct CharInfo(ICharObject CharObject, IReadOnlyRunProperty RunProperty)
{
}