namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 表示一个 <see cref="IImmutableRun"/> 列表
/// </summary>
internal interface IImmutableRunList
{
    int RunCount { get; }
    IImmutableRun GetRun(int index);
}