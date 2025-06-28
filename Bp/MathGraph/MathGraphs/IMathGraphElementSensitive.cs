namespace MathGraph;

public interface IMathGraphElementSensitive<TElementInfo, TEdgeInfo>
{
    MathGraphElement<TElementInfo, TEdgeInfo> MathGraphElement { set; get; }
}