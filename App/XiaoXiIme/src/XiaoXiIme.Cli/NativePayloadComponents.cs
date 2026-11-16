namespace XiaoXiIme.Cli;

internal sealed record NativePayloadComponents
    (string RuntimeIdentifier, string ImeFile, string TsfModule, string TsfAbiHostExecutable);