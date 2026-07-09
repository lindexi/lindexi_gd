using XiaoXiIme.Foundation;

namespace XiaoXiIme.Dictionary;

public interface IImeDictionary
{
    IReadOnlyList<ImeCandidate> Query(string reading, int maxCount = 9);
}