using System.Linq;
using UnoInk.Inking.InkCore;

namespace NarjejerechowainoBuwurjofear.Inking.Primitive;

public readonly record struct StylusPointListSpan(IReadOnlyList<StylusPoint> OriginList, int Start, int Length)
{
    public IEnumerable<StylusPoint> GetEnumerable()
    {
        return OriginList.Skip(Start).Take(Length);
    }

    public IReadOnlyList<StylusPoint> ToReadOnlyList()
    {
        var result = new StylusPoint[Length];
        for (int i = 0, listIndex = Start; i < Length; i++, listIndex++)
        {
            result[i] = OriginList[listIndex];
        }

        return result;
    }
}