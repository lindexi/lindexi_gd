using System.Diagnostics;

namespace MathGraphs;

public readonly record struct MathGraphEqualsResult(MathGraphNotEqualsReason Reason)
{
    public bool IsEquals => Reason == MathGraphNotEqualsReason.Equals;
}


public enum MathGraphNotEqualsReason
{
    Equals,
    ElementListCountNotEquals,
    ElementNotEquals,
    EdgeListCountNotEquals,
    EdgeNotEquals,
    ElementInElementListNotEquals,
    ElementOutElementListNotEquals
}

public static class MathGraphExtensions
{
    public static MathGraphEqualsResult Equals<TElementInfo, TEdgeInfo>(this MathGraph<TElementInfo, TEdgeInfo> a,
        MathGraph<TElementInfo, TEdgeInfo> b)
    {
        if (a.ElementList.Count != b.ElementList.Count)
        {
            return NotEquals(MathGraphNotEqualsReason.ElementListCountNotEquals);
        }

        Debug.Assert(a.ElementList.Count == b.ElementList.Count);
        for (var i = 0; i < a.ElementList.Count; i++)
        {
            var elementA = a.ElementList[i];
            var elementB = b.ElementList[i];

            var result = ElementEquals(elementA, elementB);
            if (!result.IsEquals)
            {
                return result;
            }

            if (elementA.InElementList.Count != elementB.InElementList.Count)
            {
                return NotEquals(MathGraphNotEqualsReason.ElementInElementListNotEquals);
            }

            result = ElementListEquals(elementA.InElementList, elementB.InElementList);
            if (!result.IsEquals)
            {
                return result;
            }

            if (elementA.OutElementList.Count != elementB.OutElementList.Count)
            {
                return NotEquals(MathGraphNotEqualsReason.ElementOutElementListNotEquals);
            }

            result = ElementListEquals(elementA.OutElementList, elementB.OutElementList);
            if (!result.IsEquals)
            {
                return result;
            }

            result = EdgeListEquals(elementA.EdgeList, elementB.EdgeList);
            if (!result.IsEquals)
            {
                return result;
            }
        }

        return Equals();
    }

    private static MathGraphEqualsResult ElementListEquals<TElementInfo, TEdgeInfo>(IReadOnlyList<MathGraphElement<TElementInfo, TEdgeInfo>> a, IReadOnlyList<MathGraphElement<TElementInfo, TEdgeInfo>> b)
    {
        Debug.Assert(a.Count == b.Count);
        for (var i = 0; i < a.Count; i++)
        {
            var result = ElementEquals(a[i], b[i]);
            if (!result.IsEquals)
            {
                return result;
            }
        }

        return Equals();
    }

    private static MathGraphEqualsResult ElementEquals<TElementInfo, TEdgeInfo>(MathGraphElement<TElementInfo, TEdgeInfo> a, MathGraphElement<TElementInfo, TEdgeInfo> b)
    {
        if (a.Value is MathGraph<string, string> aMathGraph && b.Value is MathGraph<string, string> bMathGraph)
        {
            return Equals(aMathGraph, bMathGraph);
        }

        if (a.Id != b.Id)
        {
            return NotEquals(MathGraphNotEqualsReason.ElementNotEquals);
        }

        if (EqualityComparer<TElementInfo>.Default.Equals(a.Value, b.Value))
        {
            return Equals();
        }
        else
        {
            return NotEquals(MathGraphNotEqualsReason.ElementNotEquals);
        }

        //Debug.Assert(EqualityComparer<TElementInfo>.Default.Equals(a.Value, b.Value));
        //Debug.Assert(a.Id == b.Id);
    }

    private static MathGraphEqualsResult EdgeListEquals<TElementInfo, TEdgeInfo>(IReadOnlyList<MathGraphEdge<TElementInfo, TEdgeInfo>> aEdgeList,
        IReadOnlyList<MathGraphEdge<TElementInfo, TEdgeInfo>> bEdgeList)
    {
        if (aEdgeList.Count != bEdgeList.Count)
        {
            return NotEquals(MathGraphNotEqualsReason.EdgeListCountNotEquals);
        }

        Debug.Assert(aEdgeList.Count == bEdgeList.Count);

        for (var i = 0; i < aEdgeList.Count; i++)
        {
            var result = EdgeEquals(aEdgeList[i], bEdgeList[i]);
            if (!result.IsEquals)
            {
                return result;
            }
        }

        return Equals();
    }

    private static MathGraphEqualsResult EdgeEquals<TElementInfo, TEdgeInfo>(MathGraphEdge<TElementInfo, TEdgeInfo> a, MathGraphEdge<TElementInfo, TEdgeInfo> b)
    {
        if (!EqualityComparer<TEdgeInfo>.Default.Equals(a.EdgeInfo, b.EdgeInfo))
        {
            return NotEquals(MathGraphNotEqualsReason.EdgeNotEquals);
        }

        Debug.Assert(EqualityComparer<TEdgeInfo>.Default.Equals(a.EdgeInfo, b.EdgeInfo));

        if (a.GetType() != b.GetType())
        {
            return NotEquals(MathGraphNotEqualsReason.EdgeNotEquals);
        }

        Debug.Assert(a.GetType() == b.GetType());

        if (a is MathGraphBidirectionalEdge<TElementInfo, TEdgeInfo> aBidirectionalEdge &&
            b is MathGraphBidirectionalEdge<TElementInfo, TEdgeInfo> bBidirectionalEdge)
        {
            var result = ElementEquals(aBidirectionalEdge.AElement, bBidirectionalEdge.AElement);
            if (!result.IsEquals)
            {
                return result;
            }

            result = ElementEquals(aBidirectionalEdge.BElement, bBidirectionalEdge.BElement);
            if (!result.IsEquals)
            {
                return result;
            }
        }
        else if (a is MathGraphUnidirectionalEdge<TElementInfo, TEdgeInfo> aUnidirectionalEdge &&
                 b is MathGraphUnidirectionalEdge<TElementInfo, TEdgeInfo> bUnidirectionalEdge)
        {
            var result = ElementEquals(aUnidirectionalEdge.From, bUnidirectionalEdge.From);
            if (!result.IsEquals)
            {
                return result;
            }

            result = ElementEquals(aUnidirectionalEdge.To, bUnidirectionalEdge.To);
            if (!result.IsEquals)
            {
                return result;
            }
        }
        else
        {
            return NotEquals(MathGraphNotEqualsReason.EdgeNotEquals);
        }

        return Equals();
    }

    private static MathGraphEqualsResult NotEquals(MathGraphNotEqualsReason reason) =>
        new MathGraphEqualsResult(reason);
    private static MathGraphEqualsResult Equals() => new MathGraphEqualsResult(MathGraphNotEqualsReason.Equals);
}