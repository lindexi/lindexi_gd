namespace XiaoXiIme.Dictionary;

public sealed record ImeDictionaryQuery(
    string Input,
    int MaxCount = 9,
    ImeDictionaryMatchMode MatchMode = ImeDictionaryMatchMode.Exact)
{
    public string Input
    {
        get;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            field = value;
        }
    } = Input ?? throw new ArgumentNullException(nameof(Input));
}
