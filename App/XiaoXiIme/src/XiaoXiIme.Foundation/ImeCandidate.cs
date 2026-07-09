namespace XiaoXiIme.Foundation;

public sealed record ImeCandidate(
    string Text,
    string Reading,
    int Score = 0);