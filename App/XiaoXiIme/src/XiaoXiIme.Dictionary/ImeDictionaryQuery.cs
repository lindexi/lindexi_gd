namespace XiaoXiIme.Dictionary;

public sealed record ImeDictionaryQuery(
    string Input,
    int MaxCount = 9,
    ImeDictionaryMatchMode MatchMode = ImeDictionaryMatchMode.Exact);
