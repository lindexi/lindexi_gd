using XiaoXiIme.Foundation;

namespace XiaoXiIme.Dictionary;

/// <summary>
/// Describes a selected candidate together with the original input that produced it.
/// </summary>
public sealed record ImeDictionaryLearning(ImeCandidate Candidate, string OriginalInput)
{
    public ImeCandidate Candidate
    {
        get;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            field = value;
        }
    } = Candidate ?? throw new ArgumentNullException(nameof(Candidate));

    public string OriginalInput
    {
        get;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            field = value;
        }
    } = OriginalInput ?? throw new ArgumentNullException(nameof(OriginalInput));
}
