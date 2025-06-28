namespace MathGraphs;

/// <summary>
/// 双向边
/// </summary>
/// <typeparam name="TElementInfo"></typeparam>
/// <typeparam name="TEdgeInfo"></typeparam>
public class MathGraphBidirectionalEdge<TElementInfo, TEdgeInfo> : MathGraphEdge<TElementInfo, TEdgeInfo>
{
    public MathGraphBidirectionalEdge(MathGraphElement<TElementInfo, TEdgeInfo> a,
        MathGraphElement<TElementInfo, TEdgeInfo> b) : base(a, b)
    {
    }

    /// <summary>
    /// 双向边中的一个元素
    /// </summary>
    public MathGraphElement<TElementInfo, TEdgeInfo> AElement => base.A;

    /// <summary>
    /// 双向边中的另一个元素
    /// </summary>
    public MathGraphElement<TElementInfo, TEdgeInfo> BElement => base.B;

    public override string ToString()
    {
        return $"[{AElement}] <-> [{BElement}]";
    }
}