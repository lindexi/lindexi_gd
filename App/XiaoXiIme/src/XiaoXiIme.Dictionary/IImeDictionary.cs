using XiaoXiIme.Foundation;

namespace XiaoXiIme.Dictionary;

public interface IImeDictionary
{
    IReadOnlyList<ImeCandidate> Query(ImeDictionaryQuery query);

    IReadOnlyList<ImeCandidate> Query(string reading, int maxCount = 9)
    {
        return Query(new ImeDictionaryQuery(reading, maxCount));
    }
}