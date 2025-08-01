namespace LightTextEditorPlus.Core.Diagnostics.LogInfos;

/// <summary>
/// 在更新布局过程中，文档发生了变更的日志信息
/// </summary>
/// <remarks>
/// 一般随后会带上 <see cref="TextEditorBeDirtyAfterLayoutCompletedLogInfo"/> 日志信息
/// 可以在日志里面判断此类型添加断点，通过调用堆栈了解是哪个业务模块变更
/// </remarks>
public readonly record struct DocumentChangedWhenUpdatingLayoutLogInfo
{
    /// <inheritdoc />
    public override string ToString() =>
        $"[TextEditorCore][DocumentChanged] 在更新布局过程中，文档发生了变更。";
}