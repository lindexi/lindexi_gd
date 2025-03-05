namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 字符信息。由于 <see cref="CharData"/> 太大了，包含了布局等信息，因此准备将其削弱为 <see cref="CharInfo"/> 结构体。刚好结构体传递也是比较免费的情况，比 <see cref="CharObjectSpanTextRun"/> 能少一毛钱的损耗
/// </summary>
/// <param name="CharObject"></param>
/// <param name="RunProperty"></param>
public readonly record struct CharInfo(ICharObject CharObject, IReadOnlyRunProperty RunProperty)
{
}