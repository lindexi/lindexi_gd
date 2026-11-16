using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeCore.Tests;

internal static class CanonicalDictionaryFixture
{
    internal static InMemoryImeDictionary Create(IEnumerable<ImeCandidate> entries) => new(new ImeLexicon(
        entries.Select(e => new ImePhoneticEntry(e.Text, e.Reading, e.Score)).ToArray(),
        [], ImeProjectionDefinition.FullPinyin, 1));
}
