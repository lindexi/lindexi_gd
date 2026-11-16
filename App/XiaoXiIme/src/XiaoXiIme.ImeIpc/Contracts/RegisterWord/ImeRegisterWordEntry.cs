namespace XiaoXiIme.ImeIpc;

public sealed record ImeRegisterWordEntry
(
    string Reading,
    string Text,
    uint Style = 0
);