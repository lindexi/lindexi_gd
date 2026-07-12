namespace XiaoXiIme.Cli;

internal interface IImeInstaller
{
    ImeInstallationResult Install(string imeFilePath, string displayName);
}

internal readonly record struct ImeInstallationResult(bool Succeeded, string Message);
