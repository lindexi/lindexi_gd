using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Core.Rendering;

/// <summary>
/// 段落的行渲染信息
/// </summary>
/// <param name="LineIndex">这一行是段落的第几行，从0开始</param>
/// <param name="Argument">行渲染参数</param>
public readonly record struct ParagraphLineRenderInfo(int LineIndex, LineDrawingArgument Argument)
{
    /// <summary>
    /// 内部使用的行信息
    /// </summary>
    /// 由于需要修改访问权限，修改为属性
    internal LineLayoutData LineLayoutData { init; get; } = null!;

    /// <summary>
    /// 设置渲染结果
    /// </summary>
    /// <param name="lineDrawnResult"></param>
    public void SetDrawnResult(in LineDrawnResult lineDrawnResult)
    {
        LineLayoutData.SetDrawn(lineDrawnResult);
    }
}