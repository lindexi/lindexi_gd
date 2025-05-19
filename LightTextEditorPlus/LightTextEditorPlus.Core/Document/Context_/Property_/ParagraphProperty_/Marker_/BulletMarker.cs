namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 无符号项目符号，又称无序项目符号
/// </summary>
public sealed class BulletMarker : TextMarker
{
    /// <summary>
    /// 项目符号文本
    /// </summary>
    public string? MarkerText { get; init; }

    internal override void DisableInherit()
    {
    }
}