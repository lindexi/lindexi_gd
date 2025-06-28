namespace MathGraph;

/// <summary>
/// 单向边
/// </summary>
/// <typeparam name="TElementInfo"></typeparam>
/// <typeparam name="TEdgeInfo"></typeparam>
public class MathGraphUnidirectionalEdge<TElementInfo, TEdgeInfo> : MathGraphEdge<TElementInfo, TEdgeInfo>
{
    /// <summary>
    /// 创建单向边
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    public MathGraphUnidirectionalEdge(MathGraphElement<TElementInfo, TEdgeInfo> from,
        MathGraphElement<TElementInfo, TEdgeInfo> to) : base(from, to)
    {
    }

    /// <summary>
    /// 单向边的起点元素
    /// </summary>
    public MathGraphElement<TElementInfo, TEdgeInfo> From => base.A;

    /// <summary>
    /// 单向边的终点元素
    /// </summary>
    public MathGraphElement<TElementInfo, TEdgeInfo> To => base.B;

    public override string ToString()
    {
        return $"[{From}] -> [{To}]";
    }
}