namespace MathGraphs;

/// <summary>
/// 表示元素的数据结构中，如果对图里的元素（点）本身感兴趣，则可以实现此接口。就会在加入图时被注入 <see cref="MathGraphElement"/> 属性
/// </summary>
/// <typeparam name="TElementInfo"></typeparam>
/// <typeparam name="TEdgeInfo"></typeparam>
public interface IMathGraphElementSensitive<TElementInfo, TEdgeInfo>
{
    MathGraphElement<TElementInfo, TEdgeInfo> MathGraphElement { set; get; }
}