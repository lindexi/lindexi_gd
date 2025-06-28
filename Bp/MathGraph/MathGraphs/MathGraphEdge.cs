namespace MathGraphs;

/// <summary>
/// 表示图的边，包含两个元素，A和B。A和B可以是同一个元素，但不能是null。
/// </summary>
/// <typeparam name="TElementInfo"></typeparam>
/// <typeparam name="TEdgeInfo"></typeparam>
public abstract class MathGraphEdge<TElementInfo, TEdgeInfo>
{
    protected MathGraphEdge(MathGraphElement<TElementInfo, TEdgeInfo> a, MathGraphElement<TElementInfo, TEdgeInfo> b)
    {
        A = a;
        B = b;
    }

    public TEdgeInfo? EdgeInfo { get; set; }

    /// <summary>
    /// 边的两端中的一个元素
    /// </summary>
    protected MathGraphElement<TElementInfo, TEdgeInfo> A { get; }

    /// <summary>
    /// 边的两端中的另一个元素
    /// </summary>
    protected MathGraphElement<TElementInfo, TEdgeInfo> B { get; }

    /// <summary>
    /// 确保元素在边的两端之一
    /// </summary>
    /// <param name="element"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public void EnsureContain(MathGraphElement<TElementInfo, TEdgeInfo> element)
    {
        if (!ReferenceEquals(element, A) && !ReferenceEquals(element, B))
        {
            throw new InvalidOperationException();
        }
    }

    /// <summary>
    /// 传入边的这一个元素，返回边的另一个元素
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException">如果传入的元素不属于边的两端两个元素中的一个</exception>
    public MathGraphElement<TElementInfo, TEdgeInfo> GetOtherElement(MathGraphElement<TElementInfo, TEdgeInfo> element)
    {
        if (ReferenceEquals(element, A))
        {
            return B;
        }

        if (ReferenceEquals(element, B))
        {
            return A;
        }

        throw new InvalidOperationException();
    }
}