namespace DotNetCampus.Inking.Primitive;

public readonly record struct StylusPointListSpan(IReadOnlyList<InkStylusPoint> OriginList, int Start, int Length)
{
    public IEnumerable<InkStylusPoint> GetEnumerable()
    {
        return OriginList.Skip(Start).Take(Length);
    }

    public IReadOnlyList<InkStylusPoint> ToReadOnlyList()
    {
        var result = new InkStylusPoint[Length];
        for (int i = 0, listIndex = Start; i < Length; i++, listIndex++)
        {
            result[i] = OriginList[listIndex];
        }

        return result;
    }
}