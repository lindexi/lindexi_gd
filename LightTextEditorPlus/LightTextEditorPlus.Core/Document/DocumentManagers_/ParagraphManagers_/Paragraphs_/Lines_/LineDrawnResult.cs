namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 行的渲染结果
/// </summary>
/// <param name="LineAssociatedRenderData">行的关联渲染信息。下次更新渲染，将会自动带上，传入到 <see cref="LineDrawingArgument.LineAssociatedRenderData"/> 参数</param>
public readonly record struct LineDrawnResult(object? LineAssociatedRenderData)
{
}